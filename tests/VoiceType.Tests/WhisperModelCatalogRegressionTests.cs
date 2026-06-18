using VoiceType.Models;
using VoiceType.Utils;

namespace VoiceType.Tests;

public class WhisperModelCatalogRegressionTests
{
    [Theory]
    [InlineData(WhisperModelSize.TinyEn, "ggml-tiny.en.bin", "Tiny English (fastest)")]
    [InlineData(WhisperModelSize.BaseEn, "ggml-base.en.bin", "Base English (recommended)")]
    [InlineData(WhisperModelSize.SmallEn, "ggml-small.en.bin", "Small English (more accurate)")]
    public void Catalog_maps_model_size_to_file_and_label(
        WhisperModelSize size, string fileName, string label)
    {
        Assert.Equal(fileName, WhisperModelCatalog.FileNameFor(size));
        Assert.Equal(label, WhisperModelCatalog.NameFor(size));
        Assert.True(WhisperModelCatalog.Models.ContainsKey(label));
        Assert.Equal(size, WhisperModelCatalog.Models[label]);
    }

    [Fact]
    public void Catalog_includes_all_expected_model_options()
    {
        Assert.Equal(5, WhisperModelCatalog.Models.Count);
        Assert.Contains("Tiny English (fastest)", WhisperModelCatalog.Models.Keys);
        Assert.Contains("Base Multilingual", WhisperModelCatalog.Models.Keys);
    }
}
