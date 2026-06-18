using VoiceType.Tests.Support;
using VoiceType.Utils;

namespace VoiceType.Tests;

public class WavAudioNormalizerRegressionTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* best effort */ }
        }
    }

    private string Track(string path)
    {
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public void PrepareForSpeech_returns_same_path_for_16khz_mono_pcm()
    {
        var wav = Track(TestWavFactory.CreateSilentWav(TimeSpan.FromMilliseconds(500)));
        var log = new FakeLogService();

        var prepared = WavAudioNormalizer.PrepareForSpeech(wav, log);

        Assert.Equal(wav, prepared);
    }

    [Fact]
    public void PrepareForSpeech_rejects_clips_shorter_than_350ms()
    {
        var wav = Track(TestWavFactory.CreateSilentWav(TimeSpan.FromMilliseconds(200)));
        var log = new FakeLogService();

        var ex = Assert.Throws<InvalidOperationException>(
            () => WavAudioNormalizer.PrepareForSpeech(wav, log));

        Assert.Contains("too short", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrepareForSpeech_normalizes_non_target_sample_rate()
    {
        var wav = Track(TestWavFactory.CreateSilentWav(TimeSpan.FromMilliseconds(500), sampleRate: 44100));
        var log = new FakeLogService();

        var prepared = WavAudioNormalizer.PrepareForSpeech(wav, log);
        _tempFiles.Add(prepared);

        Assert.NotEqual(wav, prepared);
        Assert.True(File.Exists(prepared));
    }
}
