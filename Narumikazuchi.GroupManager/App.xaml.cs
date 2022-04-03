using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!Configuration.TryLoad())
        {
            ConfigurationWindow.Window window = new();
            window.ShowDialog();
            if (!window.DialogResult
                       .HasValue ||
                !window.DialogResult
                       .Value)
            {
                MessageBox.Show(messageBoxText: "The application can't run without a valid configuration, shutting down.",
                                caption: "Invalid configuration",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Error);
                this.Shutdown();
                return;
            }
        }
        if (!Preferences.TryLoad())
        {
            Preferences.Current
                       .Save();
        }
        String path = Path.Combine(Environment.CurrentDirectory,
                                   $"{Preferences.Current.Locale}.locale");
        FileInfo file = new(path);
        if (Preferences.Current.Locale != "en" &&
            !file.Exists)
        {
            MessageBox.Show(messageBoxText: "The localization file couldn't be found. The language default (english) will be displayed.",
                            caption: "File not found",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Asterisk);
        }
        else if (Preferences.Current.Locale != "en" && 
                 !Localization.TryLoad(file))
        {
            MessageBox.Show(messageBoxText: "The localization file couldn't be loaded. The language default (english) will be displayed.",
                            caption: "File not found",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Asterisk);
        }

        this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        this.MainWindow = new MainWindow.Window();
        this.MainWindow
            .Show();
    }
}