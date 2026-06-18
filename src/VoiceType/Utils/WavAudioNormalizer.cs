using System.IO;
using NAudio.Wave;
using VoiceType.Services;

namespace VoiceType.Utils;

/// <summary>
/// Ensures captured WAV files are in a format System.Speech accepts and contain
/// enough audio to transcribe.
/// </summary>
public static class WavAudioNormalizer
{
    private static readonly WaveFormat TargetFormat = new(16000, 16, 1);
    private static readonly TimeSpan MinDuration = TimeSpan.FromMilliseconds(350);

    public static string PrepareForSpeech(string wavPath, ILogService log)
    {
        if (!File.Exists(wavPath))
            throw new FileNotFoundException("Recording file was not found.", wavPath);

        using var reader = new WaveFileReader(wavPath);
        var duration = reader.TotalTime;
        log.Info(
            $"WAV input: rate={reader.WaveFormat.SampleRate}, " +
            $"bits={reader.WaveFormat.BitsPerSample}, channels={reader.WaveFormat.Channels}, " +
            $"duration={duration.TotalMilliseconds:F0}ms");

        if (duration < MinDuration)
        {
            throw new InvalidOperationException(
                $"Recording too short ({duration.TotalMilliseconds:F0} ms). " +
                "Hold the hotkey longer while speaking.");
        }

        if (IsTargetFormat(reader.WaveFormat))
            return wavPath;

        var normalized = Path.Combine(Path.GetTempPath(), $"voicetype_norm_{Guid.NewGuid():N}.wav");
        reader.Position = 0;
        using var conversion = new WaveFormatConversionStream(TargetFormat, reader);
        using (var writer = new WaveFileWriter(normalized, conversion.WaveFormat))
        {
            conversion.CopyTo(writer);
        }
        log.Info($"Normalized WAV written: {normalized}");
        return normalized;
    }

    private static bool IsTargetFormat(WaveFormat format) =>
        format.Encoding == WaveFormatEncoding.Pcm
        && format.SampleRate == TargetFormat.SampleRate
        && format.BitsPerSample == TargetFormat.BitsPerSample
        && format.Channels == TargetFormat.Channels;
}
