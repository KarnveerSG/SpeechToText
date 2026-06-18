using System.IO;
using System.Windows;
using VoiceType.Services;
using VoiceType.ViewModels;
using VoiceType.Views;

namespace VoiceType.Tests;

public class UiSmokeTests
{
    [Fact]
    public void Settings_window_opens_without_xaml_errors()
    {
        Exception? caught = null;

        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                var tempDir = Path.Combine(Path.GetTempPath(), "VoiceType.UiTests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                var log = new LogService();
                var settings = new SettingsService(tempDir);
                var capture = new AudioCaptureService(settings, log);
                var startup = new StartupService();
                var notify = new NoopNotificationService();
                var vm = new SettingsViewModel(settings, capture, startup, notify, log);

                var window = new SettingsWindow(vm);
                window.Show();
                window.Close();

                if (log is IDisposable d)
                    d.Dispose();

                try { Directory.Delete(tempDir, true); } catch { }

                app.Shutdown();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(15));

        Assert.Null(caught);
    }

    private sealed class NoopNotificationService : INotificationService
    {
        public void Info(string title, string message) { }
        public void Error(string title, string message) { }
    }
}
