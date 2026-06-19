namespace VoiceType.Utils;

/// <summary>
/// Maps friendly key names to Win32 virtual-key codes for the Settings hotkey
/// dropdown. Covers letters, digits, function keys, mouse side buttons, and
/// a few common keys.
/// </summary>
public static class KeyCatalog
{
    public const uint VkXButton1 = 0x05; // Mouse back (often labelled button 4)
    public const uint VkXButton2 = 0x06; // Mouse forward (often labelled button 5)

    public static readonly IReadOnlyDictionary<string, uint> Keys = Build();

    public static bool IsMouseButton(uint virtualKey) =>
        virtualKey is VkXButton1 or VkXButton2;

    private static Dictionary<string, uint> Build()
    {
        var d = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            ["Space"] = 0x20,
            ["Enter"] = 0x0D,
            ["Tab"] = 0x09,
            ["Backquote (`)"] = 0xC0,
            ["Caps Lock"] = 0x14,
            ["Insert"] = 0x2D,
            ["Delete"] = 0x2E,
            ["Home"] = 0x24,
            ["End"] = 0x23,
            // Side mouse buttons — numbering varies by vendor; try both if unsure.
            ["Mouse 4 (Forward)"] = VkXButton2,
            ["Mouse 5 (Back)"] = VkXButton1,
            ["Mouse Forward (Side)"] = VkXButton2,
            ["Mouse Back (Side)"] = VkXButton1,
        };

        for (char c = 'A'; c <= 'Z'; c++)
            d[c.ToString()] = c;

        for (char c = '0'; c <= '9'; c++)
            d[c.ToString()] = c;

        for (uint i = 1; i <= 12; i++)
            d[$"F{i}"] = 0x70 + (i - 1);

        return d;
    }

    public static string NameForVk(uint vk)
    {
        foreach (var kv in Keys)
            if (kv.Value == vk) return kv.Key;

        return vk switch
        {
            VkXButton2 => "Mouse 4 (Forward)",
            VkXButton1 => "Mouse 5 (Back)",
            _ => "Space"
        };
    }
}
