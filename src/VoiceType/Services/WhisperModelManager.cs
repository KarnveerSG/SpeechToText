using System.IO;
using Whisper.net;
using Whisper.net.Ggml;
using VoiceType.Models;
using VoiceType.Utils;
using static VoiceType.Utils.WhisperNativeBootstrap;

namespace VoiceType.Services;

/// <summary>
/// Downloads, caches, and loads ggml Whisper models from
/// <c>%APPDATA%\VoiceType\models</c>.
/// </summary>
public sealed class WhisperModelManager : IDisposable
{
    private readonly ISettingsService _settings;
    private readonly ILogService _log;
    private readonly INotificationService _notify;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private WhisperFactory? _factory;
    private string? _loadedModelPath;
    private volatile bool _disposed;

    public WhisperModelManager(
        ISettingsService settings,
        ILogService log,
        INotificationService notify)
    {
        _settings = settings;
        _log = log;
        _notify = notify;
        ModelsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VoiceType",
            "models");
        Directory.CreateDirectory(ModelsDirectory);

        _settings.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (!_disposed)
            Invalidate();
    }

    public string ModelsDirectory { get; }

    public int ThreadCount { get; } = Math.Max(1, Environment.ProcessorCount / 2);

    public async Task<WhisperFactory> GetFactoryAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Initialize();

        var size = _settings.Current.WhisperModel;
        var path = Path.Combine(ModelsDirectory, WhisperModelCatalog.FileNameFor(size));

        if (!File.Exists(path))
            await EnsureModelAsync(size, path, ct).ConfigureAwait(false);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_factory is null || !string.Equals(_loadedModelPath, path, StringComparison.OrdinalIgnoreCase))
            {
                _factory?.Dispose();
                _factory = WhisperFactory.FromPath(path);
                _loadedModelPath = path;
                _log.Info($"Loaded local Whisper model: {path}");
            }

            return _factory;
        }
        finally
        {
            if (!_disposed)
                _gate.Release();
        }
    }

    public WhisperProcessorBuilder CreateProcessorBuilder(WhisperFactory factory) =>
        factory.CreateBuilder()
            .WithLanguage("en")
            .WithThreads(ThreadCount);

    public void Invalidate()
    {
        if (_disposed)
            return;

        if (!_gate.Wait(TimeSpan.FromSeconds(5)))
            return;

        try
        {
            if (_disposed)
                return;

            _factory?.Dispose();
            _factory = null;
            _loadedModelPath = null;
        }
        finally
        {
            if (!_disposed)
                _gate.Release();
        }
    }

    private async Task EnsureModelAsync(WhisperModelSize size, string path, CancellationToken ct)
    {
        var ggml = MapGgmlType(size);
        var label = WhisperModelCatalog.NameFor(size);
        var approx = WhisperModelCatalog.ApproximateSize(size);

        _log.Info($"Downloading Whisper model {label} ({approx})...");
        _notify.Info("Downloading speech model",
            $"{label} ({approx}) — one-time download, runs fully offline after.");

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_disposed || File.Exists(path))
                return;

            var temp = path + ".download";
            if (File.Exists(temp))
            {
                try { File.Delete(temp); }
                catch { /* retry download */ }
            }

            await using (var modelStream = await WhisperGgmlDownloader.Default
                .GetGgmlModelAsync(ggml, cancellationToken: ct).ConfigureAwait(false))
            await using (var fileWriter = File.Create(temp))
            {
                await modelStream.CopyToAsync(fileWriter, ct).ConfigureAwait(false);
            }

            File.Move(temp, path);
            _log.Info($"Whisper model saved to {path}");
            _notify.Info("Speech model ready", "Local transcription is set up — no API key needed.");
        }
        finally
        {
            if (!_disposed)
                _gate.Release();
        }
    }

    private static GgmlType MapGgmlType(WhisperModelSize size) => size switch
    {
        WhisperModelSize.TinyEn => GgmlType.TinyEn,
        WhisperModelSize.SmallEn => GgmlType.SmallEn,
        WhisperModelSize.BaseEn => GgmlType.BaseEn,
        WhisperModelSize.Small => GgmlType.Small,
        WhisperModelSize.Base => GgmlType.Base,
        _ => GgmlType.BaseEn
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _settings.SettingsChanged -= OnSettingsChanged;

        if (_gate.Wait(TimeSpan.FromSeconds(5)))
        {
            try
            {
                _factory?.Dispose();
                _factory = null;
            }
            finally
            {
                try { _gate.Release(); }
                catch (ObjectDisposedException) { /* shutting down */ }
            }
        }

        _gate.Dispose();
    }
}
