using VoiceType.Models;
using VoiceType.Services;

namespace VoiceType.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VoiceType.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup for temp test data.
        }
    }

    [Fact]
    public void SaveAndLoad_PersistsSettingsToDisk()
    {
        var service = new SettingsService(_tempDir);
        service.Current.Hotkey = new HotkeyConfig
        {
            Modifiers = HotkeyModifiers.Alt,
            VirtualKey = 0x56,
            KeyName = "V"
        };
        service.Current.Engine = SttEngine.Offline;
        service.Current.WhisperModel = WhisperModelSize.SmallEn;
        service.Save();

        var reloaded = new SettingsService(_tempDir);

        Assert.Equal(HotkeyModifiers.Alt, reloaded.Current.Hotkey.Modifiers);
        Assert.Equal(0x56u, reloaded.Current.Hotkey.VirtualKey);
        Assert.Equal("V", reloaded.Current.Hotkey.KeyName);
        Assert.Equal(SttEngine.Offline, reloaded.Current.Engine);
        Assert.Equal(WhisperModelSize.SmallEn, reloaded.Current.WhisperModel);
    }

    [Fact]
    public void NotifyChanged_SavesAndRaisesSettingsChanged()
    {
        var service = new SettingsService(_tempDir);
        var changed = false;
        service.SettingsChanged += (_, _) => changed = true;

        service.Current.PlayBeep = false;
        service.NotifyChanged();

        Assert.True(changed);
        Assert.False(new SettingsService(_tempDir).Current.PlayBeep);
    }
}
