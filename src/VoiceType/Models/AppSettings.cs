namespace VoiceType.Models;

/// <summary>
/// Which speech-to-text backend to use. Legacy field — the app always uses local
/// Whisper now; Cloud is ignored.
/// </summary>
public enum SttEngine
{
    /// <summary>Local Whisper.cpp (free, on-device).</summary>
    Offline,

    /// <summary>Deprecated — no longer used.</summary>
    Cloud
}

/// <summary>
/// How transcribed text is delivered to the foreground window.
/// </summary>
public enum DeliveryMode
{
    /// <summary>Copy to clipboard and send Ctrl+V (fast, reliable for most apps).</summary>
    Paste,

    /// <summary>Type the text character-by-character (works where paste is blocked).</summary>
    Type
}

/// <summary>
/// Persisted configuration for the entire application. Serialized to
/// <c>%APPDATA%\VoiceType\settings.json</c>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>The configured push-to-talk hotkey.</summary>
    public HotkeyConfig Hotkey { get; set; } = HotkeyConfig.Default;

    /// <summary>
    /// NAudio WaveIn device number for the chosen microphone. -1 = system default.
    /// </summary>
    public int MicrophoneDeviceNumber { get; set; } = -1;

    /// <summary>Friendly product name of the chosen microphone (for display only).</summary>
    public string MicrophoneName { get; set; } = "Default";

    /// <summary>Which STT engine to use (legacy; local Whisper is always used).</summary>
    public SttEngine Engine { get; set; } = SttEngine.Offline;

    /// <summary>Local Whisper model size stored under AppData.</summary>
    public WhisperModelSize WhisperModel { get; set; } = WhisperModelSize.BaseEn;

    /// <summary>Legacy OpenAI key — no longer used.</summary>
    public string CloudApiKey { get; set; } = string.Empty;

    /// <summary>Whether to launch VoiceType when the user logs into Windows.</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Whether to play the start/stop/error audio cues.</summary>
    public bool PlayBeep { get; set; } = true;

    /// <summary>How to deliver the transcribed text.</summary>
    public DeliveryMode Delivery { get; set; } = DeliveryMode.Paste;

    /// <summary>Manual theme override. null = follow system theme.</summary>
    public bool? DarkMode { get; set; }
}
