using VoiceType.Models;

namespace VoiceType.Tests;

public class AppSettingsRegressionTests
{
    [Fact]
    public void Defaults_use_local_whisper_base_english()
    {
        var settings = new AppSettings();

        Assert.Equal(SttEngine.Offline, settings.Engine);
        Assert.Equal(WhisperModelSize.BaseEn, settings.WhisperModel);
        Assert.Equal(DeliveryMode.Paste, settings.Delivery);
        Assert.True(settings.PlayBeep);
    }

    [Fact]
    public void Hotkey_default_is_ctrl_alt_space()
    {
        var hotkey = HotkeyConfig.Default;

        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, hotkey.Modifiers);
        Assert.Equal(0x20u, hotkey.VirtualKey);
        Assert.Equal("Space", hotkey.KeyName);
        Assert.Equal("Ctrl + Alt + Space", hotkey.ToString());
    }
}
