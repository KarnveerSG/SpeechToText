using NAudio.Wave;

namespace VoiceType.Tests.Support;

internal static class TestWavFactory
{
    public static string CreateSilentWav(TimeSpan duration, int sampleRate = 16000, short channels = 1)
    {
        var path = Path.Combine(Path.GetTempPath(), $"voicetype-test-{Guid.NewGuid():N}.wav");
        var format = new WaveFormat(sampleRate, 16, channels);
        using var writer = new WaveFileWriter(path, format);

        var sampleCount = (int)(sampleRate * channels * duration.TotalSeconds);
        var buffer = new byte[sampleCount * 2];
        writer.Write(buffer, 0, buffer.Length);
        return path;
    }
}
