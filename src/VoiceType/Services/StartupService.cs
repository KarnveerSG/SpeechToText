using Microsoft.Win32;

namespace VoiceType.Services;

/// <summary>Manages the "start with Windows" auto-run registry entry.</summary>
public interface IStartupService
{
    bool IsEnabled();
    void SetEnabled(bool enabled);
}

/// <summary>
/// Toggles a value under
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>. This is per-user
/// and needs no elevation.
/// </summary>
public sealed class StartupService : IStartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "VoiceType";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch
        {
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key is null) return;

            if (enabled)
            {
                var exe = Environment.ProcessPath
                          ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe))
                    key.SetValue(ValueName, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Registry access can fail under locked-down policies; ignore.
        }
    }
}
