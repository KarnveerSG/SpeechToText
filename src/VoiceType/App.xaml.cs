using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using VoiceType.Services;
using VoiceType.Utils;
using VoiceType.ViewModels;
using VoiceType.Views;
using Application = System.Windows.Application;

namespace VoiceType;

public partial class App : Application
{
    private NotifyIcon? _trayIcon;
    private IServiceProvider? _services;
    private DictationCoordinator? _coordinator;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VoiceType", "logs");
                Directory.CreateDirectory(logDir);
                var logPath = Path.Combine(logDir, $"voicetype-crash-{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logPath, $"{DateTime.Now:O} UI ERROR: {args.Exception}\r\n");
            }
            catch { /* ignore */ }

            System.Windows.MessageBox.Show(
                $"VoiceType hit an error:\n\n{args.Exception.Message}",
                "VoiceType",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        CreateTrayIcon();

        var notify = _services.GetRequiredService<INotificationService>() as NotificationService;
        notify?.Attach(_trayIcon!);

        _coordinator = _services.GetRequiredService<DictationCoordinator>();
        _coordinator.Start();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<IAudioCueService, AudioCueService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<WhisperModelManager>();
        services.AddSingleton<LocalWhisperSpeechToTextService>();
        services.AddSingleton<ISpeechToTextService>(sp =>
            sp.GetRequiredService<LocalWhisperSpeechToTextService>());
        services.AddSingleton<SpeechToTextResolver>();
        services.AddSingleton<ITextInjectionService, TextInjectionService>();
        services.AddSingleton<DictationCoordinator>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = AppIcon.CreateTrayIcon(),
            Text = "VoiceType - Push-to-talk dictation",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add("Open Log Folder", null, (_, _) => OpenLogFolder());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => Shutdown());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowSettings();
    }

    private void ShowSettings()
    {
        if (_services is null)
            return;

        Dispatcher.Invoke(() =>
        {
            try
            {
                var settingsWindow = _services.GetRequiredService<SettingsWindow>();
                settingsWindow.ShowDialog();

                var hotkey = _services.GetRequiredService<IHotkeyService>();
                hotkey.Reload();
            }
            catch (Exception ex)
            {
                _services.GetRequiredService<ILogService>().Error("Failed to open Settings", ex);
                System.Windows.MessageBox.Show(
                    $"Could not open Settings:\n\n{ex.Message}",
                    "VoiceType",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        });
    }

    private void OpenLogFolder()
    {
        if (_services is null)
            return;

        try
        {
            var log = _services.GetRequiredService<ILogService>();
            Process.Start(new ProcessStartInfo
            {
                FileName = log.LogDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _services.GetRequiredService<ILogService>().Error("Failed to open log folder", ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _coordinator?.Dispose();

        if (_services is IDisposable disposable)
            disposable.Dispose();

        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
