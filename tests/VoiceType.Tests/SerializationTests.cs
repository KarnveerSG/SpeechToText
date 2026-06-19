using System.Text.Json;
using VoiceType.Models;

namespace VoiceType.Tests;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [Fact]
    public void HotkeyConfig_RoundTripsThroughJson()
    {
        var original = new HotkeyConfig
        {
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift,
            VirtualKey = 0x41,
            KeyName = "A"
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var restored = JsonSerializer.Deserialize<HotkeyConfig>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Equal(original.Modifiers, restored.Modifiers);
        Assert.Equal(original.VirtualKey, restored.VirtualKey);
        Assert.Equal(original.KeyName, restored.KeyName);
        Assert.Equal("Ctrl + Shift + A", restored.ToString());
    }

    [Fact]
    public void AppSettings_RoundTripsThroughJson()
    {
        var original = new AppSettings
        {
            Hotkey = new HotkeyConfig
            {
                Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
                VirtualKey = 0x20,
                KeyName = "Space"
            },
            MicrophoneDeviceNumber = 2,
            MicrophoneName = "USB Mic",
            Engine = SttEngine.Offline,
            WhisperModel = WhisperModelSize.BaseEn,
            CloudApiKey = "legacy-unused",
            StartWithWindows = true,
            PlayBeep = false,
            Delivery = DeliveryMode.Type,
            DarkMode = true
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var restored = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Equal(original.MicrophoneDeviceNumber, restored.MicrophoneDeviceNumber);
        Assert.Equal(original.MicrophoneName, restored.MicrophoneName);
        Assert.Equal(original.Engine, restored.Engine);
        Assert.Equal(original.WhisperModel, restored.WhisperModel);
        Assert.Equal(original.CloudApiKey, restored.CloudApiKey);
        Assert.Equal(original.StartWithWindows, restored.StartWithWindows);
        Assert.Equal(original.PlayBeep, restored.PlayBeep);
        Assert.Equal(original.Delivery, restored.Delivery);
        Assert.Equal(original.DarkMode, restored.DarkMode);
        Assert.Equal(original.Hotkey.Modifiers, restored.Hotkey.Modifiers);
        Assert.Equal(original.Hotkey.VirtualKey, restored.Hotkey.VirtualKey);
        Assert.Equal(original.Hotkey.KeyName, restored.Hotkey.KeyName);
    }
}
