using System.Windows.Forms;

namespace VoiceType.Services;

/// <summary>Shows brief, non-intrusive notifications to the user.</summary>
public interface INotificationService
{
    void Info(string title, string message);
    void Error(string title, string message);
}

/// <summary>
/// Balloon-tip notifications via the tray icon. Every notification is also
/// written to the log file for troubleshooting.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogService _log;
    private NotifyIcon? _icon;

    public NotificationService(ILogService log)
    {
        _log = log;
    }

    /// <summary>Wired up by the tray service once its NotifyIcon exists.</summary>
    public void Attach(NotifyIcon icon) => _icon = icon;

    public void Info(string title, string message)
    {
        _log.Info($"Notification: {title} — {message}");
        Show(title, message, ToolTipIcon.Info);
    }

    public void Error(string title, string message)
    {
        _log.Warning($"Notification: {title} — {message}");
        Show(title, message, ToolTipIcon.Error);
    }

    private void Show(string title, string message, ToolTipIcon icon)
    {
        if (_icon is null)
            return;

        try
        {
            _icon.BalloonTipTitle = title;
            _icon.BalloonTipText = message;
            _icon.BalloonTipIcon = icon;
            _icon.ShowBalloonTip(4000);
        }
        catch (Exception ex)
        {
            _log.Warning($"Failed to show balloon tip: {ex.Message}");
        }
    }
}
