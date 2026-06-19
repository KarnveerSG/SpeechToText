using VoiceType.Models;

namespace VoiceType.Utils;

/// <summary>Display names and ggml filenames for local Whisper models.</summary>
public static class WhisperModelCatalog
{
    public static readonly IReadOnlyDictionary<string, WhisperModelSize> Models =
        new Dictionary<string, WhisperModelSize>(StringComparer.OrdinalIgnoreCase)
        {
            ["Tiny English (fastest)"] = WhisperModelSize.TinyEn,
            ["Base English (recommended)"] = WhisperModelSize.BaseEn,
            ["Small English (more accurate)"] = WhisperModelSize.SmallEn,
            ["Base Multilingual"] = WhisperModelSize.Base,
            ["Small Multilingual (faster)"] = WhisperModelSize.Small,
        };

    public static string NameFor(WhisperModelSize size)
    {
        foreach (var kv in Models)
            if (kv.Value == size) return kv.Key;
        return "Base English (recommended)";
    }

    public static string FileNameFor(WhisperModelSize size) => size switch
    {
        WhisperModelSize.TinyEn => "ggml-tiny.en.bin",
        WhisperModelSize.SmallEn => "ggml-small.en.bin",
        WhisperModelSize.BaseEn => "ggml-base.en.bin",
        WhisperModelSize.Small => "ggml-small.bin",
        WhisperModelSize.Base => "ggml-base.bin",
        _ => "ggml-base.en.bin"
    };

    public static string ApproximateSize(WhisperModelSize size) => size switch
    {
        WhisperModelSize.TinyEn => "~75 MB",
        WhisperModelSize.SmallEn => "~190 MB",
        WhisperModelSize.BaseEn => "~150 MB",
        WhisperModelSize.Small => "~480 MB",
        WhisperModelSize.Base => "~150 MB",
        _ => "~150 MB"
    };
}
