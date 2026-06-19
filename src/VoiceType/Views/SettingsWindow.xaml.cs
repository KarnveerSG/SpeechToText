using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using VoiceType.Utils;
using VoiceType.ViewModels;

namespace VoiceType.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        AppIcon.ApplyTo(this);

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        TryEnableMica();
        PlayEntranceAnimation();
    }

    private void PlayEntranceAnimation()
    {
        if (Resources["EntranceStoryboard"] is Storyboard storyboard)
            storyboard.Begin(this);
        else
            ContentRoot.Opacity = 1;
    }

    private void ApplyTheme()
    {
        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF5, 0xF5, 0xF7));
        WindowChrome.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xE6, 0x0A, 0x0A, 0x0A));
    }

    private void TryEnableMica()
    {
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int useDark = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

            int backdrop = 3;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            Background = System.Windows.Media.Brushes.Transparent;
        }
        catch { /* older Windows */ }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SaveCommand.CanExecute(null))
            _vm.SaveCommand.Execute(null);

        if (_vm.LastSaveSucceeded)
        {
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
