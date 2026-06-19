using System.IO;
using System.Text;
using VoiceType.Utils;

namespace VoiceType.Services;

/// <summary>
/// Free, on-device transcription using OpenAI Whisper via whisper.cpp
/// (Whisper.net). Handles punctuation and natural speech without any API key.
/// </summary>
public sealed class LocalWhisperSpeechToTextService : ISpeechToTextService
{
    private readonly WhisperModelManager _models;
    private readonly ILogService _log;

    public LocalWhisperSpeechToTextService(WhisperModelManager models, ILogService log)
    {
        _models = models;
        _log = log;
    }

    public async Task<string> TranscribeAsync(string wavPath, CancellationToken ct = default)
    {
        var preparedPath = WavAudioNormalizer.PrepareForSpeech(wavPath, _log);
        var deletePrepared = !string.Equals(preparedPath, wavPath, StringComparison.OrdinalIgnoreCase);

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var factory = await _models.GetFactoryAsync(ct).ConfigureAwait(false);

            using var processor = _models.CreateProcessorBuilder(factory).Build();

            var sb = new StringBuilder();
            await using var stream = File.OpenRead(preparedPath);

            await foreach (var segment in processor.ProcessAsync(stream, ct).ConfigureAwait(false))
            {
                if (!string.IsNullOrEmpty(segment.Text))
                    sb.Append(segment.Text);
            }

            var text = sb.ToString().Trim();
            sw.Stop();
            _log.Info(
                $"Local Whisper finished in {sw.ElapsedMilliseconds}ms, " +
                $"threads={_models.ThreadCount}, chars={text.Length}");
            return text;
        }
        finally
        {
            if (deletePrepared)
            {
                try { if (File.Exists(preparedPath)) File.Delete(preparedPath); }
                catch { /* best effort */ }
            }
        }
    }
}
