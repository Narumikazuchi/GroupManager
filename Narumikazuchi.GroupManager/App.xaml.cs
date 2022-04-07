namespace Narumikazuchi.GroupManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeWatcher.Instance
                    .StartThemeWatching();

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

        if (Preferences.Current
                       .Locale is null)
        {
            Localization.Instance
                        .Locale = Configuration.Current
                                               .DefaultLocale;
        }
        else
        {
            Localization.Instance
                        .Locale = Preferences.Current
                                             .Locale;
        }

        if (Localization.Instance
                        .Locale != "en" &&
            !Localization.TryLoad())
        {
            MessageBox.Show(messageBoxText: "The localization file couldn't be found. The language default (english) will be displayed.",
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