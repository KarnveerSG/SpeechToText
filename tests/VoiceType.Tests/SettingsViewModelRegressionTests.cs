using VoiceType.Models;
using VoiceType.Services;
using VoiceType.Tests.Support;
using VoiceType.Utils;
using VoiceType.ViewModels;

namespace VoiceType.Tests;

public class SettingsViewModelRegressionTests
{
    private static SettingsViewModel CreateVm(
        InMemorySettingsService settings,
        out FakeNotificationService notify)
    {
        notify = new FakeNotificationService();
        return new SettingsViewModel(
            settings,
            new FakeAudioCaptureService(),
            new FakeStartupService(),
            notify,
            new FakeLogService());
    }

    [Fact]
    public void Save_with_mouse_forward_hotkey_and_no_modifiers_succeeds()
    {
        var settings = new InMemorySettingsService();
        var vm = CreateVm(settings, out var notify);

        vm.Ctrl = false;
        vm.Shift = false;
        vm.Alt = false;
        vm.Win = false;
        vm.SelectedKey = "Mouse Forward (Side)";

        vm.SaveCommand.Execute(null);

        Assert.True(vm.LastSaveSucceeded);
        Assert.Empty(notify.Infos);
        Assert.Equal(KeyCatalog.VkXButton2, settings.Current.Hotkey.VirtualKey);
        Assert.Equal(HotkeyModifiers.None, settings.Current.Hotkey.Modifiers);
    }

    [Fact]
    public void Save_with_plain_space_and_no_modifiers_is_rejected()
    {
        var settings = new InMemorySettingsService();
        var vm = CreateVm(settings, out var notify);

        vm.Ctrl = false;
        vm.Shift = false;
        vm.Alt = false;
        vm.Win = false;
        vm.SelectedKey = "Space";

        vm.SaveCommand.Execute(null);

        Assert.False(vm.LastSaveSucceeded);
        Assert.Contains(notify.Infos, n => n.Title == "Pick a modifier or mouse button");
    }

    [Fact]
    public void Save_persists_whisper_model_and_local_engine()
    {
        var settings = new InMemorySettingsService();
        var vm = CreateVm(settings, out _);

        vm.Ctrl = true;
        vm.SelectedKey = "A";
        vm.SelectedWhisperModel = "Tiny English (fastest)";
        vm.TypeInsteadOfPaste = true;

        vm.SaveCommand.Execute(null);

        Assert.True(vm.LastSaveSucceeded);
        Assert.Equal(WhisperModelSize.TinyEn, settings.Current.WhisperModel);
        Assert.Equal(SttEngine.Offline, settings.Current.Engine);
        Assert.Equal(DeliveryMode.Type, settings.Current.Delivery);
        Assert.Equal(1, settings.SaveCount);
    }

    [Fact]
    public void Key_catalog_includes_mouse_side_buttons()
    {
        var settings = new InMemorySettingsService();
        var vm = CreateVm(settings, out _);

        Assert.Contains("Mouse 4 (Forward)", vm.KeyNames);
        Assert.Contains("Mouse Forward (Side)", vm.KeyNames);
        Assert.Contains("Tiny English (fastest)", vm.WhisperModelNames);
        Assert.Contains("Base English (recommended)", vm.WhisperModelNames);
    }

    [Fact]
    public void Hotkey_preview_updates_for_ctrl_alt_space()
    {
        var settings = new InMemorySettingsService();
        var vm = CreateVm(settings, out _);

        vm.Ctrl = true;
        vm.Alt = true;
        vm.SelectedKey = "Space";

        Assert.Equal("Ctrl + Alt + Space", vm.HotkeyPreview);
    }
}
