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

    public ICommand SaveConfigurationCommand =>
        m_SaveConfigurationCommand;
}

partial class ViewModel : WindowViewModel
{
    private void SaveConfiguration(Window window)
    {
        if (!ActiveDirectoryInterface.TryGetOU(dn: this.UserOuDn,
                                               ou: out _))
        {
            MessageBox.Show("Error");
            return;
        }
        if (!ActiveDirectoryInterface.TryGetOU(dn: this.GroupOuDn,
                                               ou: out _))
        {
            MessageBox.Show("Error");
            return;
        }

        Configuration configuration = new()
        {
            UserOuDn = this.UserOuDn,
            GroupOuDn = this.GroupOuDn
        };
        Configuration.Current = configuration;
        configuration.Save();
        window.Close();
    }

    private readonly RelayCommand<Window> m_SaveConfigurationCommand;
    private String m_UserOuDn = String.Empty;
    private String m_GroupOuDn = String.Empty;
}