using System.Diagnostics;
using System.Runtime.InteropServices;
using VoiceType.Models;
using VoiceType.Utils;

namespace VoiceType.Services;

/// <summary>
/// Watches a global push-to-talk hotkey and raises press/release events even
/// when the app has no focus.
/// </summary>
public interface IHotkeyService
{
    event EventHandler? HotkeyPressed;
    event EventHandler? HotkeyReleased;
    void Start();
    void Reload();
    void Stop();
}

/// <summary>
/// Global push-to-talk via low-level keyboard and mouse hooks. Keyboard keys
/// use WH_KEYBOARD_LL; side mouse buttons use WH_MOUSE_LL (XButton1/XButton2).
/// </summary>
public sealed class HotkeyService : IHotkeyService, IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_XBUTTONUP = 0x020C;

    private const int VK_LCONTROL = 0xA2, VK_RCONTROL = 0xA3;
    private const int VK_LSHIFT = 0xA0, VK_RSHIFT = 0xA1;
    private const int VK_LMENU = 0xA4, VK_RMENU = 0xA5;
    private const int VK_LWIN = 0x5B, VK_RWIN = 0x5C;

    private readonly ISettingsService _settings;
    private readonly INotificationService _notify;
    private readonly ILogService _log;
    private readonly LowLevelKeyboardProc _keyboardProc;
    private readonly LowLevelMouseProc _mouseProc;

    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;

    private HotkeyConfig _config;
    private bool _isActive;

    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;

    public HotkeyService(ISettingsService settings, INotificationService notify, ILogService log)
    {
        _settings = settings;
        _notify = notify;
        _log = log;
        _config = settings.Current.Hotkey;
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;
    }

    public void Start()
    {
        InstallKeyboardHook();
        SyncMouseHook();
    }

    public void Reload()
    {
        _config = _settings.Current.Hotkey;
        _isActive = false;
        SyncMouseHook();
        _log.Info($"Hotkey config reloaded: {_config}.");
    }

    public void Stop()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
    }

    private void InstallKeyboardHook()
    {
        if (_keyboardHookId != IntPtr.Zero)
            return;

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _keyboardHookId = SetWindowsHookEx(
            WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(module.ModuleName!), 0);

        if (_keyboardHookId == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            var message = $"Failed to install keyboard hook (Win32 error {err}). Push-to-talk will not work.";
            _log.Error(message);
            _notify.Error("Hotkey hook failed", message);
        }
        else
        {
            _log.Info($"Keyboard hook installed. Listening for {_config}.");
        }
    }

    private void SyncMouseHook()
    {
        bool needMouse = KeyCatalog.IsMouseButton(_config.VirtualKey);

        if (needMouse && _mouseHookId == IntPtr.Zero)
            InstallMouseHook();
        else if (!needMouse && _mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
    }

    private void InstallMouseHook()
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _mouseHookId = SetWindowsHookEx(
            WH_MOUSE_LL, _mouseProc, GetModuleHandle(module.ModuleName!), 0);

        if (_mouseHookId == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            var message = $"Failed to install mouse hook (Win32 error {err}). Side mouse buttons will not work.";
            _log.Error(message);
            _notify.Error("Mouse hook failed", message);
        }
        else
        {
            _log.Info($"Mouse hook installed for {_config.KeyName}.");
        }
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            int vkCode = Marshal.ReadInt32(lParam);

            if (msg is WM_KEYDOWN or WM_SYSKEYDOWN)
                EvaluateState(vkCode, isDown: true);
            else if (msg is WM_KEYUP or WM_SYSKEYUP)
                EvaluateState(vkCode, isDown: false);
        }

        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            if (msg is WM_XBUTTONDOWN or WM_XBUTTONUP)
            {
                var hook = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int xButton = (hook.mouseData >> 16) & 0xFFFF;
                int vk = xButton switch
                {
                    1 => (int)KeyCatalog.VkXButton1,
                    2 => (int)KeyCatalog.VkXButton2,
                    _ => 0
                };

                if (vk != 0)
                    EvaluateState(vk, isDown: msg == WM_XBUTTONDOWN);
            }
        }

        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void EvaluateState(int changedVk, bool isDown)
    {
        if (!KeyCatalog.IsMouseButton(_config.VirtualKey) && changedVk == (int)_config.VirtualKey)
        {
            // Keyboard trigger — handled below via main key check.
        }
        else if (KeyCatalog.IsMouseButton(_config.VirtualKey) && changedVk != (int)_config.VirtualKey)
        {
            // Ignore unrelated mouse buttons; still refresh modifier state on keyboard events.
            if (!IsModifierKey(changedVk))
                return;
        }

        bool mainKeyPressed;
        if (changedVk == (int)_config.VirtualKey)
            mainKeyPressed = isDown;
        else
            mainKeyPressed = IsKeyDown((int)_config.VirtualKey);

        bool modifiersOk = ModifiersSatisfied();
        bool nowActive = mainKeyPressed && modifiersOk;

        if (nowActive && !_isActive)
        {
            _isActive = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
        else if (!nowActive && _isActive)
        {
            _isActive = false;
            HotkeyReleased?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool IsModifierKey(int vk) =>
        vk is VK_LCONTROL or VK_RCONTROL or VK_LSHIFT or VK_RSHIFT
            or VK_LMENU or VK_RMENU or VK_LWIN or VK_RWIN;

    private bool ModifiersSatisfied()
    {
        if (_config.Modifiers == HotkeyModifiers.None)
            return true;

        bool NeedAndHas(HotkeyModifiers flag, params int[] vks)
        {
            if (!_config.Modifiers.HasFlag(flag))
                return true;
            return vks.Any(IsKeyDown);
        }

        return NeedAndHas(HotkeyModifiers.Control, VK_LCONTROL, VK_RCONTROL)
            && NeedAndHas(HotkeyModifiers.Shift, VK_LSHIFT, VK_RSHIFT)
            && NeedAndHas(HotkeyModifiers.Alt, VK_LMENU, VK_RMENU)
            && NeedAndHas(HotkeyModifiers.Win, VK_LWIN, VK_RWIN);
    }

    private static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    public void Dispose() => Stop();

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
