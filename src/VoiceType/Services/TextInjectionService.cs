using System.Windows;
using WindowsInput;
using WindowsInput.Native;
using VoiceType.Models;

namespace VoiceType.Services;

/// <summary>Delivers transcribed text into the foreground window.</summary>
public interface ITextInjectionService
{
    void Deliver(string text);
}

/// <summary>
/// Uses InputSimulatorPlus to deliver text. In Paste mode the text is placed on
/// the clipboard and Ctrl+V is sent (fast, preserves unicode). In Type mode each
/// character is typed, which works in apps that block programmatic paste.
/// </summary>
public sealed class TextInjectionService : ITextInjectionService
{
    private readonly ISettingsService _settings;
    private readonly IInputSimulator _input = new InputSimulator();

    public TextInjectionService(ISettingsService settings)
    {
        _settings = settings;
    }

    public void Deliver(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (_settings.Current.Delivery == DeliveryMode.Type)
        {
            _input.Keyboard.TextEntry(text);
            return;
        }

        // Paste mode: stash existing clipboard, set ours, Ctrl+V, restore.
        string? previous = null;
        try
        {
            previous = TryGetClipboardText();
            SetClipboardText(text);

            // Small delay so the clipboard is settled before paste.
            Thread.Sleep(40);
            _input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            Thread.Sleep(60);
        }
        finally
        {
            if (previous is not null)
            {
                try { SetClipboardText(previous); } catch { /* ignore */ }
            }
        }
    }

    private static string? TryGetClipboardText()
    {
        string? result = null;
        RunOnSta(() =>
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                    result = System.Windows.Clipboard.GetText();
            }
            catch { /* ignore */ }
        });
        return result;
    }

    private static void SetClipboardText(string text)
    {
        RunOnSta(() =>
        {
            try { System.Windows.Clipboard.SetText(text); } catch { /* ignore */ }
        });
    }

    /// <summary>Clipboard APIs require an STA thread.</summary>
    private static void RunOnSta(Action action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            action();
            return;
        }

        var t = new Thread(() => action());
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
    }
}
