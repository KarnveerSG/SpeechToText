using System.Text;

namespace VoiceType.Models;

/// <summary>
/// Modifier flags matching the Win32 RegisterHotKey fsModifiers values.
/// </summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None = 0x0000,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

/// <summary>
/// A fully described push-to-talk hotkey: a set of modifiers plus a single
/// non-modifier key. Stored as Win32 virtual key codes so the values can be fed
/// directly to RegisterHotKey and matched against low-level keyboard hook events.
/// </summary>
public sealed class HotkeyConfig
{
    /// <summary>Bitmask of modifier keys that must be held.</summary>
    public HotkeyModifiers Modifiers { get; set; } = HotkeyModifiers.Control | HotkeyModifiers.Alt;

    /// <summary>Win32 virtual-key code of the main key. Default = VK_SPACE (0x20).</summary>
    public uint VirtualKey { get; set; } = 0x20;

    /// <summary>Human friendly name of the main key (e.g. "Space", "A", "F5").</summary>
    public string KeyName { get; set; } = "Space";

    public static HotkeyConfig Default => new();

    /// <summary>Renders the hotkey for display, e.g. "Ctrl + Alt + Space".</summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) sb.Append("Ctrl + ");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) sb.Append("Shift + ");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) sb.Append("Alt + ");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) sb.Append("Win + ");
        sb.Append(string.IsNullOrEmpty(KeyName) ? "?" : KeyName);
        return sb.ToString();
    }
}
