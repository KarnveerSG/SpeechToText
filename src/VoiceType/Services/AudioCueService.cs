using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoiceType.Services;

/// <summary>Plays the short start/stop/error tones described in the spec.</summary>
public interface IAudioCueService
{
    void PlayStart();
    void PlayStop();
    void PlayError();
}

/// <summary>
/// Soft sine chimes with fade envelopes — pleasant to the ear and never
/// recorded into the mic capture stream.
/// </summary>
public sealed class AudioCueService : IAudioCueService, IDisposable
{
    private const int SampleRate = 44100;
    private const double FadeInSeconds = 0.012;
    private const double FadeOutSeconds = 0.035;

    private readonly ISettingsService _settings;
    private readonly ILogService _log;

    public AudioCueService(ISettingsService settings, ILogService log)
    {
        _settings = settings;
        _log = log;
    }

    /// <summary>Gentle rising major third — mic is live.</summary>
    public void PlayStart() =>
        PlayMelody([(523.25, 95), (659.25, 110)], gain: 0.16);

    /// <summary>Soft descending pair — processing started.</summary>
    public void PlayStop() =>
        PlayMelody([(659.25, 85), (523.25, 100)], gain: 0.16);

    /// <summary>Low, muted double pulse — something went wrong.</summary>
    public void PlayError() =>
        PlayMelody([(349.23, 120), (0, 45), (293.66, 120)], gain: 0.13);

    private void PlayMelody((double frequencyHz, int durationMs)[] notes, double gain)
    {
        if (!_settings.Current.PlayBeep)
            return;

        Task.Run(() =>
        {
            try
            {
                var segments = new List<ISampleProvider>(notes.Length);
                foreach (var (frequencyHz, durationMs) in notes)
                {
                    if (frequencyHz <= 0 || durationMs <= 0)
                    {
                        segments.Add(Silence(durationMs));
                        continue;
                    }

                    segments.Add(FadedTone(frequencyHz, durationMs, gain));
                }

                using var output = new WaveOutEvent();
                output.Init(new ConcatenatingSampleProvider(segments));
                output.Play();
                while (output.PlaybackState == PlaybackState.Playing)
                    Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                _log.Warning($"Audio cue playback failed: {ex.Message}");
            }
        });
    }

    private static ISampleProvider FadedTone(double frequencyHz, int durationMs, double gain)
    {
        var tone = new SignalGenerator(SampleRate, 1)
        {
            Gain = gain,
            Frequency = frequencyHz,
            Type = SignalGeneratorType.Sin
        }.Take(TimeSpan.FromMilliseconds(durationMs));

        var fadeIn = (int)(FadeInSeconds * SampleRate);
        var fadeOut = (int)(FadeOutSeconds * SampleRate);
        var total = (long)(SampleRate * (durationMs / 1000.0));
        return new EnvelopeSampleProvider(tone, fadeIn, fadeOut, total);
    }

    private static ISampleProvider Silence(int durationMs)
    {
        return new SignalGenerator(SampleRate, 1)
        {
            Gain = 0,
            Frequency = 440,
            Type = SignalGeneratorType.Sin
        }.Take(TimeSpan.FromMilliseconds(durationMs));
    }

    /// <summary>Applies a short fade-in/out so tones do not click.</summary>
    private sealed class EnvelopeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly int _fadeInSamples;
        private readonly int _fadeOutSamples;
        private readonly long _totalSamples;
        private long _position;

        public EnvelopeSampleProvider(
            ISampleProvider source, int fadeInSamples, int fadeOutSamples, long totalSamples)
        {
            _source = source;
            _fadeInSamples = Math.Max(1, fadeInSamples);
            _fadeOutSamples = Math.Max(1, fadeOutSamples);
            _totalSamples = totalSamples;
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);
            for (int i = 0; i < read; i++)
            {
                long pos = _position + i;
                double env = 1.0;
                if (pos < _fadeInSamples)
                    env = pos / (double)_fadeInSamples;
                else if (pos > _totalSamples - _fadeOutSamples)
                    env = Math.Max(0, (_totalSamples - pos) / (double)_fadeOutSamples);

                buffer[offset + i] *= (float)env;
            }

            _position += read;
            return read;
        }
    }

    public void Dispose() { }
}
