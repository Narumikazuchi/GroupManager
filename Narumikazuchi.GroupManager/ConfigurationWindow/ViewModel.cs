namespace Narumikazuchi.GroupManager.ConfigurationWindow;

public sealed partial class ViewModel
{
    public ViewModel(IConfiguration configuration,
                     IActiveDirectoryService activeDirectory,
                     ILocalizationService localization)
    {
        m_ApplicationConfiguration = configuration;
        m_ActiveDirectoryService = activeDirectory;
        m_LocalizationService = localization;

        m_SaveConfigurationCommand = new(this.SaveConfiguration);
        m_DefaultLocaleChangedCommand = new(this.DefaultLocaleChanged);

        m_LocalizationService.LocaleChanged += this.UpdateLocalization;
        m_LocalizationService.LocaleListChanged += this.UpdateLocaleList;
    }

    public String PrincipalDnLabel
    {
        get
        {
            if (this.UseGroupForPrincipals)
            {
                return m_LocalizationService.LocalizationDictionary["PrincipalGroupDn"];
            }
            else
            {
                return m_LocalizationService.LocalizationDictionary["PrincipalOuDn"];
            }
        }
    }

    public Boolean UseGroupForPrincipals
    {
        get => m_UseGroupForPrincipals;
        set
        {
            if (m_UseGroupForPrincipals == value)
            {
                return;
            }
            else
            {
                m_UseGroupForPrincipals = value;
                this.OnPropertyChanged(nameof(this.UseGroupForPrincipals));
                this.OnPropertyChanged(nameof(this.PrincipalDnLabel));
            }
        }
    }

    public String PrincipalDn
    {
        get => m_PrincipalDn;
        set
        {
            m_PrincipalDn = value;
            this.OnPropertyChanged(nameof(this.PrincipalDn));
        }
    }

    public String GroupDnLabel
    {
        get
        {
            if (this.UseGroupForGroups)
            {
                return m_LocalizationService.LocalizationDictionary["GroupsGroupDn"];
            }
            else
            {
                return m_LocalizationService.LocalizationDictionary["GroupOuDn"];
            }
        }
    }

    public Boolean UseGroupForGroups
    {
        get => m_UseGroupForGroups;
        set
        {
            if (m_UseGroupForGroups == value)
            {
                return;
            }
            else
            {
                m_UseGroupForGroups = value;
                this.OnPropertyChanged(nameof(this.UseGroupForGroups));
                this.OnPropertyChanged(nameof(this.GroupDnLabel));
            }
        }
    }

    public String GroupDn
    {
        get => m_GroupDn;
        set
        {
            m_GroupDn = value;
            this.OnPropertyChanged(nameof(this.GroupDn));
        }
    }

    public String DefaultLocale
    {
        get => m_DefaultLocale;
        set
        {
            m_DefaultLocale = value;
            this.OnPropertyChanged(nameof(this.DefaultLocale));
        }
    }

    public Visibility ProgressVisibility
    {
        get => m_ProgressVisibility;
        set
        {
            m_ProgressVisibility = value;
            this.OnPropertyChanged(nameof(this.ProgressVisibility));
        }
    }

    public Configuration Result
    {
        get;
        set;
    } = new();

    public ICommand SaveConfigurationCommand =>
        m_SaveConfigurationCommand;

    public ICommand DefaultLocaleChangedCommand =>
        m_DefaultLocaleChangedCommand;

    public String Title =>
        m_LocalizationService.LocalizationDictionary["Configuration"];

    public String DefaultLocaleLabel =>
        m_LocalizationService.LocalizationDictionary["DefaultLocale"];

    public String UseGroupLabel =>
        m_LocalizationService.LocalizationDictionary["UseGroup"];

    public String SaveLabel =>
        m_LocalizationService.LocalizationDictionary["Save"];

    public String CancelLabel =>
        m_LocalizationService.LocalizationDictionary["Cancel"];

    public IEnumerable<String> AvailableLanguages =>
        m_LocalizationService.AvailableLocales;
}

partial class ViewModel : WindowViewModel
{
    private void UpdateLocalization()
    {
        this.OnPropertyChanged(nameof(this.PrincipalDnLabel));
        this.OnPropertyChanged(nameof(this.GroupDnLabel));
        this.OnPropertyChanged(nameof(this.Title));
        this.OnPropertyChanged(nameof(this.DefaultLocaleLabel));
        this.OnPropertyChanged(nameof(this.UseGroupLabel));
        this.OnPropertyChanged(nameof(this.SaveLabel));
        this.OnPropertyChanged(nameof(this.CancelLabel));
    }

    private void UpdateLocaleList() =>
        this.OnPropertyChanged(nameof(this.AvailableLanguages));

    private void SaveConfiguration(Window window)
    {
        this.ProgressVisibility = Visibility.Visible;
        Task.Run(async () => await this.SaveConfigurationAsync(window));
    }

    private async Task SaveConfigurationAsync(Window window)
    {
        Boolean success = await this.CanGetPrincipalAsync(this.PrincipalDn);
        if (!success)
        {
            window.Dispatcher
                  .Invoke(() =>
            {
                MessageBox.Show(owner: window,
                                messageBoxText: String.Format(format: m_LocalizationService.LocalizationDictionary["FailedToFindAdsObject"],
                                                              arg0: this.PrincipalDn),
                                caption: "Failed to find adsObject",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Error);
                this.ProgressVisibility = Visibility.Collapsed;
            });
            return;
        }

        success = await this.CanGetPrincipalAsync(this.GroupDn);
        if (!success)
        {
            window.Dispatcher
                  .Invoke(() =>
            {
                MessageBox.Show(owner: window,
                                messageBoxText: String.Format(format: m_LocalizationService.LocalizationDictionary["FailedToFindAdsObject"],
                                                              arg0: this.GroupDn),
                                caption: "Failed to find adsObject",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Error);
                this.ProgressVisibility = Visibility.Collapsed;
            });
            return;
        }

        //m_ApplicationConfiguration.DefaultLocale = Localization.AvailableLanguages[this.SelectedIndex];
        //m_ApplicationConfiguration.UseGroupsForPrincipals = this.UseGroupForPrincipals;
        //m_ApplicationConfiguration.PrincipalsDn = this.PrincipalDn;
        //m_ApplicationConfiguration.UseGroupsForGroups = this.UseGroupForGroups;
        //m_ApplicationConfiguration.ManagedGroupsDn = this.GroupDn;
        m_ApplicationConfiguration.CopyFrom(this.Result);
        m_ApplicationConfiguration.Save();
        window.Dispatcher
              .Invoke(() =>
        {
            window.DialogResult = true;
            window.Close();
        });
    }

    private async Task<Boolean> CanGetPrincipalAsync(String dn) => 
        await Task.Run(() =>
        {
            if (!m_ActiveDirectoryService.TryGetPrincipalByDN(distinguishedName: dn,
                                                              principal: out _))
            {
                return false;
            }
            return true;
        });

    private void DefaultLocaleChanged(ComboBox comboBox)
    {

    }

    private readonly IConfiguration m_ApplicationConfiguration;
    private readonly IActiveDirectoryService m_ActiveDirectoryService;
    private readonly ILocalizationService m_LocalizationService;
    private readonly RelayCommand<Window> m_SaveConfigurationCommand;
    private readonly RelayCommand<ComboBox> m_DefaultLocaleChangedCommand;
    private Boolean m_UseGroupForPrincipals = false;
    private String m_PrincipalDn = String.Empty;
    private Boolean m_UseGroupForGroups = false;
    private String m_GroupDn = String.Empty;
    private String m_DefaultLocale = String.Empty;
    private Visibility m_ProgressVisibility = Visibility.Collapsed;
}