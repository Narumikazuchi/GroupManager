namespace Narumikazuchi.GroupManager;

public sealed partial class ThemeWatcher
{
    public void StartThemeWatching()
    {
        try
        {
            m_WindowsTheme = GetWindowsTheme();
            MergeThemeDictionaries(m_WindowsTheme);
            m_Watcher.EventArrived += this.Watcher_EventArrived;
            SystemParameters.StaticPropertyChanged += this.SystemParameters_PropertyChanged;
            // Start listening for events
            m_Watcher.Start();
        }
        catch
        {
            // This can fail on Windows 7
            m_WindowsTheme = WindowsTheme.Default;
        }
    }

    public static WindowsTheme GetWindowsTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(REGISTRYKEYPATH);
            if (key is null)
            {
                return WindowsTheme.Light;
            }

            Object? registryValueObject = key.GetValue(REGISTRYVALUENAME);
            if (registryValueObject is null)
            {
                return WindowsTheme.Light;
            }

            Int32 registryValue = (Int32)registryValueObject;

            if (SystemParameters.HighContrast)
            {
                return WindowsTheme.Dark;
            }
            else if (registryValue > 0)
            {
                return WindowsTheme.Light;
            }
            else
            {
                return WindowsTheme.Dark;
            }
        }
        catch
        {
            return WindowsTheme.Light;
        }
    }

    public event EventHandler<ThemeWatcher, ThemeChangedEventArgs>? WindowsThemeChanged;

    public static ThemeWatcher Instance { get; } = new();

    public WindowsTheme WindowsTheme
    {
        get => m_WindowsTheme;
        set
        {
            m_WindowsTheme = value;
            MergeThemeDictionaries(m_WindowsTheme);
            this.WindowsThemeChanged?
                .Invoke(sender: this,
                        eventArgs: new(m_WindowsTheme));
        }
    }
}

// Non-Public
partial class ThemeWatcher
{
    private ThemeWatcher()
    {
        WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
        String query = String.Format(provider: CultureInfo.InvariantCulture,
                                     format: @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                                     arg0: currentUser.User!.Value,
                                     arg1: REGISTRYKEYPATH.Replace(@"\", @"\\"),
                                     arg2: REGISTRYVALUENAME);

        m_Watcher = new(query);
    }

    private static void MergeThemeDictionaries(WindowsTheme windowsTheme)
    {
        String appTheme = "Light";
        switch (windowsTheme)
        {
            case WindowsTheme.Light:
                appTheme = "Light";
                break;
            case WindowsTheme.Dark:
                appTheme = "Dark";
                break;
        }

        Application.Current
                   .Resources
                   .MergedDictionaries[0]
                   .Source = new Uri(uriString: $"/Themes/{appTheme}.xaml",
                                     uriKind: UriKind.Relative);
    }

    private void SystemParameters_PropertyChanged(Object? sender, PropertyChangedEventArgs e)
    {
        m_WindowsTheme = GetWindowsTheme();

        MergeThemeDictionaries(m_WindowsTheme);

        this.WindowsThemeChanged?
            .Invoke(sender: this,
                    eventArgs: new(m_WindowsTheme));
    }

    private void Watcher_EventArrived(Object sender, EventArrivedEventArgs e)
    {
        m_WindowsTheme = GetWindowsTheme();

        MergeThemeDictionaries(m_WindowsTheme);

        this.WindowsThemeChanged?
            .Invoke(sender: this,
                    eventArgs: new(m_WindowsTheme));
    }

    private readonly ManagementEventWatcher m_Watcher;
    private WindowsTheme m_WindowsTheme;

    private const String REGISTRYKEYPATH = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const String REGISTRYVALUENAME = "AppsUseLightTheme";
}

// IDisposable
partial class ThemeWatcher : IDisposable
{
    public void Dispose() => 
        m_Watcher.Dispose();
}