namespace VoiceType.Services;

/// <summary>Transcribes a WAV file into text.</summary>
public interface ISpeechToTextService
{
    /// <summary>
    /// Transcribes the WAV file at <paramref name="wavPath"/>. Returns the
    /// recognized text, or an empty string if nothing was recognized. Throws on
    /// hard failures (e.g. missing API key, network error) so the caller can
    /// surface a toast + error beep.
    /// </summary>
    Task<string> TranscribeAsync(string wavPath, CancellationToken ct = default);
}
