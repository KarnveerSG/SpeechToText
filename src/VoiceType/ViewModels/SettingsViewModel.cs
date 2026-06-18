using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceType.Models;
using VoiceType.Services;
using VoiceType.Utils;

namespace VoiceType.ViewModels;

/// <summary>
/// Backs the Settings window. Reads the live <see cref="AppSettings"/>, exposes
/// editable properties, and writes them back through the settings service when
/// the user saves.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IAudioCaptureService _capture;
    private readonly IStartupService _startup;
    private readonly INotificationService _notify;
    private readonly ILogService _log;

    public ObservableCollection<AudioDevice> Microphones { get; } = new();
    public ObservableCollection<string> KeyNames { get; } = new();
    public ObservableCollection<string> WhisperModelNames { get; } = new();

    // ---- Hotkey ----
    [ObservableProperty] private bool _ctrl;
    [ObservableProperty] private bool _shift;
    [ObservableProperty] private bool _alt;
    [ObservableProperty] private bool _win;
    [ObservableProperty] private string _selectedKey = "Space";

    // ---- Mic ----
    [ObservableProperty] private AudioDevice? _selectedMic;

    // ---- Whisper model ----
    [ObservableProperty] private string _selectedWhisperModel = "Base English (recommended)";

    // ---- Delivery ----
    [ObservableProperty] private bool _typeInsteadOfPaste;

    // ---- General ----
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _playBeep;

    [ObservableProperty] private string _hotkeyPreview = string.Empty;

    /// <summary>True after the most recent <see cref="Save"/> completed successfully.</summary>
    public bool LastSaveSucceeded { get; private set; }

    public SettingsViewModel(
        ISettingsService settings,
        IAudioCaptureService capture,
        IStartupService startup,
        INotificationService notify,
        ILogService log)
    {
        _settings = settings;
        _capture = capture;
        _startup = startup;
        _notify = notify;
        _log = log;

        foreach (var k in KeyCatalog.Keys.Keys.OrderBy(x => x))
            KeyNames.Add(k);

        foreach (var name in WhisperModelCatalog.Models.Keys)
            WhisperModelNames.Add(name);

        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = _settings.Current;

        Ctrl = s.Hotkey.Modifiers.HasFlag(HotkeyModifiers.Control);
        Shift = s.Hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift);
        Alt = s.Hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt);
        Win = s.Hotkey.Modifiers.HasFlag(HotkeyModifiers.Win);
        SelectedKey = KeyCatalog.NameForVk(s.Hotkey.VirtualKey);

        Microphones.Clear();
        foreach (var d in _capture.GetDevices())
            Microphones.Add(d);
        SelectedMic = Microphones.FirstOrDefault(m => m.DeviceNumber == s.MicrophoneDeviceNumber)
                      ?? Microphones.FirstOrDefault();

        SelectedWhisperModel = WhisperModelCatalog.NameFor(s.WhisperModel);
        TypeInsteadOfPaste = s.Delivery == DeliveryMode.Type;
        StartWithWindows = s.StartWithWindows || _startup.IsEnabled();
        PlayBeep = s.PlayBeep;

        UpdatePreview();
    }

    partial void OnCtrlChanged(bool value) => UpdatePreview();
    partial void OnShiftChanged(bool value) => UpdatePreview();
    partial void OnAltChanged(bool value) => UpdatePreview();
    partial void OnWinChanged(bool value) => UpdatePreview();
    partial void OnSelectedKeyChanged(string value) => UpdatePreview();

    private void UpdatePreview() => HotkeyPreview = BuildHotkey().ToString();

    private HotkeyConfig BuildHotkey()
    {
        var mods = HotkeyModifiers.None;
        if (Ctrl) mods |= HotkeyModifiers.Control;
        if (Shift) mods |= HotkeyModifiers.Shift;
        if (Alt) mods |= HotkeyModifiers.Alt;
        if (Win) mods |= HotkeyModifiers.Win;

        var keyName = SelectedKey ?? "Space";
        var vk = KeyCatalog.Keys.TryGetValue(keyName, out var v) ? v : 0x20u;

        return new HotkeyConfig { Modifiers = mods, VirtualKey = vk, KeyName = keyName };
    }

    [RelayCommand]
    private void Save()
    {
        LastSaveSucceeded = false;

        var hk = BuildHotkey();
        if (hk.Modifiers == HotkeyModifiers.None && !KeyCatalog.IsMouseButton(hk.VirtualKey))
        {
            _notify.Info("Pick a modifier or mouse button",
                "Choose at least one modifier (Ctrl/Shift/Alt/Win), or pick a side mouse button as the trigger.");
            return;
        }

        var modelName = SelectedWhisperModel ?? "Base English (recommended)";
        if (!WhisperModelCatalog.Models.TryGetValue(modelName, out var whisperModel))
            whisperModel = WhisperModelSize.BaseEn;

        var s = _settings.Current;
        s.Hotkey = hk;
        s.MicrophoneDeviceNumber = SelectedMic?.DeviceNumber ?? -1;
        s.MicrophoneName = SelectedMic?.Name ?? "Default";
        s.Engine = SttEngine.Offline;
        s.WhisperModel = whisperModel;
        s.Delivery = TypeInsteadOfPaste ? DeliveryMode.Type : DeliveryMode.Paste;
        s.PlayBeep = PlayBeep;
        s.StartWithWindows = StartWithWindows;

        _startup.SetEnabled(StartWithWindows);
        _settings.NotifyChanged();

        _log.Info(
            $"Settings saved. hotkey={hk}, mic={s.MicrophoneName}, whisper={whisperModel}, " +
            $"delivery={s.Delivery}, beep={s.PlayBeep}, startWithWindows={s.StartWithWindows}");

        LastSaveSucceeded = true;
        Saved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised after a successful save so the window can close.</summary>
    public event EventHandler? Saved;
}
