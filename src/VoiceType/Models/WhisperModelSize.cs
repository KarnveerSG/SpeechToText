namespace VoiceType.Models;

/// <summary>Local Whisper ggml model size. English-tuned models work best for English.</summary>
public enum WhisperModelSize
{
    TinyEn,
    SmallEn,
    BaseEn,
    Small,
    Base
}
