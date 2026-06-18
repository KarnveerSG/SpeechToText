using System.IO;
using System.Threading.Channels;

namespace VoiceType.Services;

/// <summary>
/// Orchestrates push-to-talk: hotkey → record → transcribe → inject text.
/// Recording can start again while a previous clip is still transcribing.
/// </summary>
public sealed class DictationCoordinator : IDisposable
{
    private readonly IHotkeyService _hotkey;
    private readonly IAudioCueService _cues;
    private readonly IAudioCaptureService _capture;
    private readonly SpeechToTextResolver _sttResolver;
    private readonly ITextInjectionService _injection;
    private readonly INotificationService _notify;
    private readonly ILogService _log;
    private readonly Channel<string> _queue;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly CancellationToken _shutdownToken;
    private readonly Task _worker;
    private int _disposed;

    public DictationCoordinator(
        IHotkeyService hotkey,
        IAudioCueService cues,
        IAudioCaptureService capture,
        SpeechToTextResolver sttResolver,
        ITextInjectionService injection,
        INotificationService notify,
        ILogService log)
    {
        _hotkey = hotkey;
        _cues = cues;
        _capture = capture;
        _sttResolver = sttResolver;
        _injection = injection;
        _notify = notify;
        _log = log;
        _shutdownToken = _shutdown.Token;

        _queue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _hotkey.HotkeyPressed += OnPressed;
        _hotkey.HotkeyReleased += OnReleased;
        _worker = Task.Run(ProcessQueueAsync);
    }

    public void Start()
    {
        _log.Info("Dictation pipeline started.");
        _hotkey.Start();
    }

    private void OnPressed(object? sender, EventArgs e)
    {
        if (_capture.IsRecording)
        {
            _log.Info("Hotkey pressed while already recording; ignored.");
            return;
        }

        try
        {
            _log.Info("Hotkey pressed — starting capture.");
            _capture.Start();
            _cues.PlayStart();
        }
        catch (Exception ex)
        {
            _log.Error("Microphone unavailable on hotkey press", ex);
            _cues.PlayError();
            _notify.Error("Microphone unavailable",
                "VoiceType couldn't access the microphone. It may be in use by another app. " + ex.Message);
        }
    }

    private void OnReleased(object? sender, EventArgs e)
    {
        if (!_capture.IsRecording)
            return;

        _log.Info("Hotkey released — stopping capture.");
        _cues.PlayStop();
        _ = EnqueueCaptureAsync();
    }

    private async Task EnqueueCaptureAsync()
    {
        try
        {
            var wavPath = await _capture.StopAsync().ConfigureAwait(false);
            if (wavPath is null)
            {
                _log.Warning("Capture finished with no usable audio file.");
                _notify.Info("Nothing captured", "No audio was recorded. Hold the button a bit longer.");
                return;
            }

            await _queue.Writer.WriteAsync(wavPath, _shutdownToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // App is shutting down.
        }
        catch (ObjectDisposedException)
        {
            // Channel or coordinator torn down during enqueue.
        }
        catch (Exception ex)
        {
            _log.Error("Failed to queue captured audio", ex);
        }
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var wavPath in _queue.Reader.ReadAllAsync(_shutdownToken).ConfigureAwait(false))
            {
                await TranscribeAndDeliverAsync(wavPath).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    private async Task TranscribeAndDeliverAsync(string wavPath)
    {
        try
        {
            var wavBytes = new FileInfo(wavPath).Length;
            _log.Info($"Captured audio: {wavPath} ({wavBytes} bytes).");

            var stt = _sttResolver.Resolve();
            var engine = stt.GetType().Name;
            _log.Info($"Transcribing with {engine}...");

            var text = await stt.TranscribeAsync(wavPath, _shutdownToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(text))
            {
                _log.Warning(
                    $"Transcription returned empty text. Engine={engine}, wavBytes={wavBytes}. " +
                    "Check mic level or hold the button longer while speaking.");
                _notify.Info("No speech detected", "VoiceType didn't recognize any words.");
                return;
            }

            _log.Info($"Transcription OK ({text.Length} chars): \"{Truncate(text, 120)}\"");
            _injection.Deliver(text);
            _log.Info("Text delivered to foreground app.");
        }
        catch (OperationCanceledException)
        {
            // App is shutting down.
        }
        catch (Exception ex)
        {
            _log.Error("Transcription pipeline failed", ex);
            _cues.PlayError();
            _notify.Error("Transcription failed", ex.Message);
        }
        finally
        {
            CleanupTemp(wavPath);
        }
    }

    private static string Truncate(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..maxChars] + "…";

    private void CleanupTemp(string? path)
    {
        if (path is null) return;
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _log.Info($"Deleted temp audio: {path}");
            }
        }
        catch (Exception ex)
        {
            _log.Warning($"Failed to delete temp audio {path}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _hotkey.HotkeyPressed -= OnPressed;
        _hotkey.HotkeyReleased -= OnReleased;

        _shutdown.Cancel();
        _queue.Writer.TryComplete();

        try { _worker.Wait(TimeSpan.FromSeconds(3)); }
        catch { /* best effort */ }

        _shutdown.Dispose();
    }
}
