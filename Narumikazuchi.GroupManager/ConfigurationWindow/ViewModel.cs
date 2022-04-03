namespace Narumikazuchi.GroupManager.ConfigurationWindow;

public sealed partial class ViewModel
{
    public ViewModel()
    {
        m_SaveConfigurationCommand = new(this.SaveConfiguration);
    }

    public String UserOuDn
    {
        get => m_UserOuDn;
        set
        {
            m_UserOuDn = value;
            this.OnPropertyChanged(nameof(this.UserOuDn));
        }
    }

    public String GroupOuDn
    {
        get => m_GroupOuDn;
        set
        {
            m_GroupOuDn = value;
            this.OnPropertyChanged(nameof(this.GroupOuDn));
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
        Boolean success = await CanGetOuAsync(this.UserOuDn);
        if (!success)
        {
            window.Dispatcher
                  .Invoke(() =>
            {
                MessageBox.Show(owner: window,
                                messageBoxText: String.Format(format: Localization.Instance
                                                                                  .FailedToOpenOU,
                                                              arg0: this.UserOuDn),
                                caption: "Failed to open OU",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Error);
                this.ProgressVisibility = Visibility.Collapsed;
            });
            return;
        }
        success = await CanGetOuAsync(this.GroupOuDn);
        if (!success)
        {
            window.Dispatcher
                  .Invoke(() =>
                  {
                      MessageBox.Show(owner: window,
                                      messageBoxText: String.Format(format: Localization.Instance
                                                                                        .FailedToOpenOU,
                                                                    arg0: this.GroupOuDn),
                                      caption: "Failed to open OU",
                                      button: MessageBoxButton.OK,
                                      icon: MessageBoxImage.Error);
                      this.ProgressVisibility = Visibility.Collapsed;
            });
            return;
        }

        Configuration configuration = new()
        {
            UserOuDn = this.UserOuDn,
            GroupOuDn = this.GroupOuDn
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

    private static async Task<Boolean> CanGetOuAsync(String dn) => 
        await Task.Run(() =>
        {
            if (!ActiveDirectoryInterface.TryGetOU(dn: dn,
                                                   ou: out _))
            {
                return false;
            }
            return true;
        });

    private readonly RelayCommand<Window> m_SaveConfigurationCommand;
    private String m_UserOuDn = String.Empty;
    private String m_GroupOuDn = String.Empty;
    private Visibility m_ProgressVisibility = Visibility.Collapsed;
}