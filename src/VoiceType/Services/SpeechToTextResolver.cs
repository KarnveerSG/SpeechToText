namespace VoiceType.Services;

/// <summary>
/// Returns the local Whisper speech-to-text service.
/// </summary>
public sealed class SpeechToTextResolver
{
    private readonly ISpeechToTextService _local;

    public SpeechToTextResolver(ISpeechToTextService local)
    {
        _local = local;
    }

    public ISpeechToTextService Resolve() => _local;
}
