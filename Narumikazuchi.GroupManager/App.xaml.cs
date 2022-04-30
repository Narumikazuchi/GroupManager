namespace Narumikazuchi.GroupManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        IServiceCollection services = new ServiceCollection();

        if (!Preferences.TryLoad(out Preferences? preferences))
        {
            preferences = new();
        }

        services.AddSingleton<ILogger, TextLogger>()
                .AddSingleton<IConfiguration, Configuration>()
                .AddSingleton<IPreferences>(preferences!)
                .AddSingleton<IActiveDirectoryService, ActiveDirectoryService>()
                .AddSingleton<ILocalizationService, LocalizationService>()
                .AddSingleton<ConfigurationWindow.ViewModel>()
                .AddSingleton<ConfigurationWindow.Window>()
                .AddSingleton<AddMemberWindow.ViewModel>()
                .AddSingleton<AddMemberWindow.Window>()
                .AddSingleton<GroupOverviewWindow.ViewModel>()
                .AddSingleton<GroupOverviewWindow.Window>()
                .AddSingleton<MainWindow.ViewModel>()
                .AddSingleton<MainWindow.Window>();

        this.ServiceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeWatcher.Instance
                    .StartThemeWatching();

        if (!Configuration.TryLoad(file: Configuration.DefaultLocation,
                                   configuration: out Configuration? configuration))
        {
            ConfigurationWindow.Window window = this.ServiceProvider
                                                    .GetService<ConfigurationWindow.Window>()!;
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
            ConfigurationWindow.ViewModel viewModel = (ConfigurationWindow.ViewModel)window.DataContext;
            configuration = viewModel.Result;
        }

        this.ServiceProvider
            .GetService<IConfiguration>()!
            .CopyFrom(configuration);

        String? locale = this.ServiceProvider
                             .GetService<IPreferences>()!
                             .Locale;

        this.ServiceProvider
            .GetService<ILocalizationService>()!
            .ChangeLocale(locale);

        this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        this.MainWindow = this.ServiceProvider
                              .GetService<MainWindow.Window>()!;
        this.MainWindow
            .Show();
    }

    public IServiceProvider ServiceProvider { get; }
}