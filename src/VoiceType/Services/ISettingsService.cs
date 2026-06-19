using VoiceType.Models;

namespace VoiceType.Services;

/// <summary>Loads and persists <see cref="AppSettings"/>.</summary>
public interface ISettingsService
{
    /// <summary>The current in-memory settings instance.</summary>
    AppSettings Current { get; }

    /// <summary>Persists <see cref="Current"/> to disk.</summary>
    void Save();

    /// <summary>Raised after settings are saved so services can react.</summary>
    event EventHandler? SettingsChanged;

    /// <summary>Notifies listeners (and saves) that settings changed.</summary>
    void NotifyChanged();
}
