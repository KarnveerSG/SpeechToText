using System.IO;
using System.Text.Json;
using VoiceType.Models;

namespace VoiceType.Services;

/// <summary>
/// JSON-file backed implementation of <see cref="ISettingsService"/>. Settings
/// live in <c>%APPDATA%\VoiceType\settings.json</c>.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly string DefaultDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VoiceType");

    private static readonly string DefaultFilePath = Path.Combine(DefaultDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _dir;
    private readonly string _filePath;

    public AppSettings Current { get; private set; }

    public event EventHandler? SettingsChanged;

    public SettingsService() : this(null) { }

    /// <summary>Production uses the default path; tests may supply a temp directory.</summary>
    internal SettingsService(string? settingsDirectoryOverride)
    {
        _dir = settingsDirectoryOverride ?? DefaultDir;
        _filePath = Path.Combine(_dir, "settings.json");
        Current = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings is not null)
                    return settings;
            }
        }
        catch
        {
            // Corrupt or unreadable settings fall back to defaults.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Best effort; ignore disk failures so the app keeps running.
        }
    }

    public void NotifyChanged()
    {
        Save();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
