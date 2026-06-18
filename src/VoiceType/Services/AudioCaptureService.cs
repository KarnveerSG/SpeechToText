using System.IO;
using NAudio.Wave;

namespace VoiceType.Services;

public sealed record AudioDevice(int DeviceNumber, string Name);

/// <summary>Captures microphone audio to a temporary WAV file.</summary>
public interface IAudioCaptureService
{
    /// <summary>Enumerates available WaveIn capture devices.</summary>
    IReadOnlyList<AudioDevice> GetDevices();

    /// <summary>
    /// Begins recording from the configured device on a background thread.
    /// Throws if the device cannot be opened (e.g. in use by another app).
    /// </summary>
    void Start();

    /// <summary>
    /// Stops recording and returns the path to the finished WAV file, or null
    /// if nothing was captured.
    /// </summary>
    Task<string?> StopAsync();

    bool IsRecording { get; }
}

/// <summary>
/// NAudio <see cref="WaveInEvent"/> based recorder. Recording runs on NAudio's
/// own callback thread, so holding the hotkey never blocks the UI. Audio is
/// written as 16 kHz mono 16-bit PCM — the format expected by both the offline
/// System.Speech recognizer and the Whisper API.
/// </summary>
public sealed class AudioCaptureService : IAudioCaptureService, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly ILogService _log;

    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _currentPath;
    private TaskCompletionSource<bool>? _stopTcs;

    public bool IsRecording { get; private set; }

    public AudioCaptureService(ISettingsService settings, ILogService log)
    {
        _settings = settings;
        _log = log;
    }

    public IReadOnlyList<AudioDevice> GetDevices()
    {
        var list = new List<AudioDevice> { new(-1, "Default") };
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            list.Add(new AudioDevice(i, caps.ProductName));
        }
        return list;
    }

    public void Start()
    {
        if (IsRecording)
            return;

        _currentPath = Path.Combine(Path.GetTempPath(), $"voicetype_{Guid.NewGuid():N}.wav");

        _waveIn = new WaveInEvent
        {
            DeviceNumber = Math.Max(-1, _settings.Current.MicrophoneDeviceNumber),
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 50
        };

        // If device number is -1 NAudio treats it as default (WAVE_MAPPER).
        if (_settings.Current.MicrophoneDeviceNumber < 0)
            _waveIn.DeviceNumber = -1;

        _writer = new WaveFileWriter(_currentPath, _waveIn.WaveFormat);

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        // StartRecording throws if the device is unavailable / in use.
        _waveIn.StartRecording();
        IsRecording = true;
        _log.Info(
            $"Recording started. device={_settings.Current.MicrophoneName} " +
            $"(#{_settings.Current.MicrophoneDeviceNumber}), file={_currentPath}");
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }
        catch
        {
            // Ignore mid-stream write errors; StopAsync will surface what we have.
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        try
        {
            _writer?.Dispose();
            _writer = null;
            _waveIn?.Dispose();
            _waveIn = null;
        }
        finally
        {
            _stopTcs?.TrySetResult(true);
        }
    }

    public async Task<string?> StopAsync()
    {
        if (!IsRecording || _waveIn is null)
            return null;

        IsRecording = false;
        _stopTcs = new TaskCompletionSource<bool>();
        _waveIn.StopRecording();

        // Wait for the RecordingStopped callback to flush and close the file.
        await _stopTcs.Task.ConfigureAwait(false);

        var path = _currentPath;
        _currentPath = null;

        if (path is not null && File.Exists(path) && new FileInfo(path).Length > 1600)
        {
            _log.Info($"Recording stopped. file={path}, bytes={new FileInfo(path).Length}");
            return path;
        }

        _log.Warning($"Recording stopped but file missing or empty: {path ?? "(null)"}");
        return null;
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _waveIn?.Dispose();
    }
}
