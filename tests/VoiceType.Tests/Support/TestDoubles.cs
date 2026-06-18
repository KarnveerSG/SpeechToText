using VoiceType.Models;
using VoiceType.Services;

namespace VoiceType.Tests.Support;

internal sealed class FakeHotkeyService : IHotkeyService
{
    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;

    public void Start() { }
    public void Reload() { }
    public void Stop() { }

    public void SimulatePress() => HotkeyPressed?.Invoke(this, EventArgs.Empty);
    public void SimulateRelease() => HotkeyReleased?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeAudioCaptureService : IAudioCaptureService
{
    private readonly Queue<string?> _stopResults = new();
    private TimeSpan _stopDelay = TimeSpan.Zero;

    public bool IsRecording { get; private set; }
    public int StartCount { get; private set; }
    public int StopCount { get; private set; }

    public IReadOnlyList<AudioDevice> GetDevices() =>
        [new AudioDevice(-1, "Default"), new AudioDevice(0, "Test Mic")];

    public void EnqueueStopResult(string? wavPath) => _stopResults.Enqueue(wavPath);

    public void SetStopDelay(TimeSpan delay) => _stopDelay = delay;

    public void Start()
    {
        StartCount++;
        IsRecording = true;
    }

    public async Task<string?> StopAsync()
    {
        StopCount++;
        IsRecording = false;

        if (_stopDelay > TimeSpan.Zero)
            await Task.Delay(_stopDelay).ConfigureAwait(false);

        return _stopResults.Count > 0 ? _stopResults.Dequeue() : null;
    }
}

internal sealed class FakeSpeechToTextService : ISpeechToTextService
{
    private readonly Queue<string> _results = new();

    public List<string> TranscribedPaths { get; } = [];
    public string NextResult { get; set; } = "hello world";
    public TimeSpan TranscribeDelay { get; set; } = TimeSpan.Zero;
    public int CallCount { get; private set; }

    public void EnqueueResult(string text) => _results.Enqueue(text);

    public async Task<string> TranscribeAsync(string wavPath, CancellationToken ct = default)
    {
        CallCount++;
        TranscribedPaths.Add(wavPath);

        if (TranscribeDelay > TimeSpan.Zero)
            await Task.Delay(TranscribeDelay, ct).ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();
        return _results.Count > 0 ? _results.Dequeue() : NextResult;
    }
}

internal sealed class FakeTextInjectionService : ITextInjectionService
{
    public List<string> Delivered { get; } = [];

    public void Deliver(string text) => Delivered.Add(text);
}

internal sealed class FakeNotificationService : INotificationService
{
    public List<(string Title, string Message)> Infos { get; } = [];
    public List<(string Title, string Message)> Errors { get; } = [];

    public void Info(string title, string message) => Infos.Add((title, message));
    public void Error(string title, string message) => Errors.Add((title, message));
}

internal sealed class FakeLogService : ILogService
{
    public string LogFilePath { get; } = Path.Combine(Path.GetTempPath(), "voicetype-test.log");
    public string LogDirectory { get; } = Path.GetTempPath();

    public List<string> Lines { get; } = [];

    public void Info(string message) => Lines.Add($"INFO {message}");
    public void Warning(string message) => Lines.Add($"WARN {message}");
    public void Error(string message, Exception? ex = null) =>
        Lines.Add(ex is null ? $"ERROR {message}" : $"ERROR {message} | {ex.Message}");
}

internal sealed class FakeAudioCueService : IAudioCueService
{
    public int StartCount { get; private set; }
    public int StopCount { get; private set; }
    public int ErrorCount { get; private set; }

    public void PlayStart() => StartCount++;
    public void PlayStop() => StopCount++;
    public void PlayError() => ErrorCount++;
}

internal sealed class FakeStartupService : IStartupService
{
    public bool Enabled { get; private set; }

    public bool IsEnabled() => Enabled;
    public void SetEnabled(bool enabled) => Enabled = enabled;
}

internal sealed class InMemorySettingsService : ISettingsService
{
    public AppSettings Current { get; } = new();
    public event EventHandler? SettingsChanged;
    public int SaveCount { get; private set; }

    public void Save()
    {
        SaveCount++;
    }

    public void NotifyChanged()
    {
        Save();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
