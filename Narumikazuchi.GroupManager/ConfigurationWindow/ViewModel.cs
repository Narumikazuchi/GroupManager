namespace Narumikazuchi.GroupManager.ConfigurationWindow;

public sealed partial class ViewModel
{
    public ViewModel()
    {
        m_SaveConfigurationCommand = new(this.SaveConfiguration);
    }

    public Int32 SelectedIndex
    {
        get;
        set;
    }

    public String PrincipalDnLabel
    {
        get
        {
            if (this.UseGroupForPrincipals)
            {
                return Localization.Instance.PrincipalGroupDn;
            }
            return Localization.Instance.PrincipalOuDn;
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
            m_UseGroupForPrincipals = value;
            this.OnPropertyChanged(nameof(this.UseGroupForPrincipals));
            this.OnPropertyChanged(nameof(this.PrincipalDnLabel));
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
                return Localization.Instance.GroupsGroupDn;
            }
            return Localization.Instance.GroupOuDn;
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
            m_UseGroupForGroups = value;
            this.OnPropertyChanged(nameof(this.UseGroupForGroups));
            this.OnPropertyChanged(nameof(this.GroupDnLabel));
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

    public ICommand SaveConfigurationCommand =>
        m_SaveConfigurationCommand;
}

partial class ViewModel : WindowViewModel
{
    private void SaveConfiguration(Window window)
    {
        this.ProgressVisibility = Visibility.Visible;
        Task.Run(async () => await this.SaveConfigurationAsync(window));
    }

    private async Task SaveConfigurationAsync(Window window)
    {
        Boolean success = await CanGetPrincipalAsync(this.PrincipalDn);
        if (!success)
        {
            window.Dispatcher
                  .Invoke(() =>
            {
                MessageBox.Show(owner: window,
                                messageBoxText: String.Format(format: Localization.Instance
                                                                                  .FailedToFindAdsObject,
                                                              arg0: this.PrincipalDn),
                                caption: "Failed to find adsObject",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Error);
                this.ProgressVisibility = Visibility.Collapsed;
            });
            return;
        }

        success = await CanGetPrincipalAsync(this.GroupDn);
        if (!success)
        {
            window.Dispatcher
                  .Invoke(() =>
            {
                MessageBox.Show(owner: window,
                                messageBoxText: String.Format(format: Localization.Instance
                                                                                  .FailedToFindAdsObject,
                                                              arg0: this.GroupDn),
                                caption: "Failed to find adsObject",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Error);
                this.ProgressVisibility = Visibility.Collapsed;
            });
            return;
        }

        Configuration configuration = new()
        {
            DefaultLocale = Localization.AvailableLanguages[this.SelectedIndex],
            UseGroupsForPrincipals = this.UseGroupForPrincipals,
            PrincipalsDn = this.PrincipalDn,
            UseGroupsForGroups = this.UseGroupForGroups,
            ManagedGroupsDn = this.GroupDn
        };
        Configuration.Current = configuration;
        configuration.Save();
        window.Dispatcher
              .Invoke(() =>
        {
            window.DialogResult = true;
            window.Close();
        });
    }

    private static async Task<Boolean> CanGetPrincipalAsync(String dn) => 
        await Task.Run(() =>
        {
            if (!ActiveDirectoryInterface.TryGetPrincipalByDN(dn: dn,
                                                              principal: out _))
            {
                return false;
            }
            return true;
        });

    private readonly RelayCommand<Window> m_SaveConfigurationCommand;
    private Boolean m_UseGroupForPrincipals = false;
    private String m_PrincipalDn = String.Empty;
    private Boolean m_UseGroupForGroups = false;
    private String m_GroupDn = String.Empty;
    private String m_DefaultLocale = String.Empty;
    private Visibility m_ProgressVisibility = Visibility.Collapsed;
}