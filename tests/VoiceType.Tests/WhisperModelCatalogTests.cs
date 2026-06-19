using VoiceType.Models;
using VoiceType.Utils;

namespace VoiceType.Tests;

public class WhisperModelCatalogTests
{
    [Theory]
    [InlineData(WhisperModelSize.TinyEn, "ggml-tiny.en.bin")]
    [InlineData(WhisperModelSize.BaseEn, "ggml-base.en.bin")]
    [InlineData(WhisperModelSize.SmallEn, "ggml-small.en.bin")]
    public void FileNameFor_ReturnsExpectedGgmlName(WhisperModelSize size, string expected)
    {
        Assert.Equal(expected, WhisperModelCatalog.FileNameFor(size));
    }

    [Fact]
    public void NameFor_RoundTripsKnownModel()
    {
        Assert.Equal("Base English (recommended)",
            WhisperModelCatalog.NameFor(WhisperModelSize.BaseEn));
    }
}
