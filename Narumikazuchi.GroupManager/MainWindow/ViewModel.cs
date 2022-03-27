namespace Narumikazuchi.GroupManager.MainWindow;

public sealed partial class ViewModel
{
    public ViewModel()
    {
        m_WindowLoadedCommand = new(this.InitializeWindow);
        m_WindowClosingCommand = new(this.CloseWindow);
        m_ReloadCommand = new(this.ReloadList);
        m_OpenSelectedItemCommand = new(this.OpenSelectedItem);
    }

    public void OpenItem(Object sender,
                         MouseButtonEventArgs eventArgs)
    {
        if (sender is not ListView view)
        {
            return;
        }
        this.OpenSelectedItem(view);
    }

    public String Manager
    {
        get => m_Manager;
        set
        {
            m_Manager = value;
            this.OnPropertyChanged(nameof(this.Manager));
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

    public ObservableCollection<GroupListItemViewModel> ManagedGroups { get; } = new();

    public ICommand WindowLoadedCommand =>
        m_WindowLoadedCommand;

    public ICommand WindowClosingCommand =>
        m_WindowClosingCommand;

    public ICommand ReloadListCommand =>
        m_ReloadCommand;

    public ICommand OpenSelectedItemCommand =>
        m_OpenSelectedItemCommand;
}

partial class ViewModel : WindowViewModel
{
    private void InitializeWindow(Window window)
    {
        Preferences preferences = Preferences.Current;
        window.Width = preferences.MainWindowSize.Width;
        window.Height = preferences.MainWindowSize.Height;

        Double left = preferences.MainWindowSize.X;
        Double top = preferences.MainWindowSize.Y;
        if (top + window.Height / 2 > SystemParameters.VirtualScreenHeight)
        {
            top = SystemParameters.VirtualScreenHeight - window.Height;
        }
        if (top < 0)
        {
            top = 0;
        }

        if (left + window.Width / 2 > SystemParameters.VirtualScreenWidth)
        {
            left = SystemParameters.VirtualScreenWidth - window.Width;
        }
        if (left < 0)
        {
            left = 0;
        }
        window.Top = top;
        window.Left = left;

        Task.Run(() => this.QueryManagedGroupsAsync());
    }

    private void CloseWindow(Window window)
    {
        Preferences preferences = Preferences.Current;
        Rect size = new(x: window.Left,
                        y: window.Top,
                        width: window.Width,
                        height: window.Height);
        preferences.MainWindowSize = size;
        preferences.Save();

        foreach (AListItemViewModel model in window.m_GroupList
                                                   .Items)
        {
            model.Dispose();
        }
    }

    private void ReloadList(Window window)
    {
        foreach (AListItemViewModel model in window.m_GroupList
                                                   .Items)
        {
            model.Dispose();
        }
        this.ManagedGroups
            .Clear();
        this.ProgressVisibility = Visibility.Visible;
        Task.Run(() => this.QueryManagedGroupsAsync());
    }

    private void OpenSelectedItem(ListView view)
    {
        if (view.SelectedItem is null ||
            view.SelectedItem is not GroupListItemViewModel group)
        {
            return;
        }

        GroupOverviewWindow.Window window = new();
        GroupOverviewWindow.ViewModel model = (GroupOverviewWindow.ViewModel)window.DataContext;
        model.Load(group);
        window.ShowDialog();
    }

    private async Task QueryManagedGroupsAsync()
    {
        // Use currently logged in user as reference
        UserPrincipal user = UserPrincipal.Current;
        if (user is not null)
        {
            this.Manager = "Manager: " + user.DisplayName;
            IReadOnlyList<DirectoryEntry> groups = await Task.Run(() => GetGroups(user.SamAccountName));
            foreach (DirectoryEntry entry in groups)
            {
                this.ManagedGroups
                    .Add(new GroupListItemViewModel(entry));
            }
        }
        else
        {
            MessageBox.Show("Konnte den angemeldeten Nutzer nicht identifizieren!", "Unknown User", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        this.ProgressVisibility = Visibility.Collapsed;
    }

    private static IReadOnlyList<DirectoryEntry> GetGroups(String samAccountName)
    {
        Configuration configuration = Configuration.Current;
        if (!ActiveDirectoryInterface.TryGetOU(dn: configuration.UserOuDn,
                                               ou: out DirectoryEntry? userOu))
        {
            MessageBox.Show("Konnte den angemeldeten Nutzer nicht finden!", "Failed to open OU", MessageBoxButton.OK, MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }
        if (!ActiveDirectoryInterface.TryGetUserBySAMAccountName(ou: userOu,
                                                                 samAccountName: samAccountName,
                                                                 out DirectoryEntry? user))
        {
            MessageBox.Show("Konnte den angemeldeten Nutzer nicht identifizieren!", "Unknown User", MessageBoxButton.OK, MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }
        if (!ActiveDirectoryInterface.TryGetOU(dn: configuration.GroupOuDn,
                                               ou: out DirectoryEntry? groupOu))
        {
            MessageBox.Show("Konnte den Gruppencontainer nicht finden!", "Failed to open OU", MessageBoxButton.OK, MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }
        if (!ActiveDirectoryInterface.TryGetGroupsManagedByUser(ou: groupOu,
                                                                user: user,
                                                                groups: out IEnumerable<DirectoryEntry>? groups))
        {
            MessageBox.Show("Konnte keine Gruppen finden!", "Group find error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }
        return new List<DirectoryEntry>(collection: groups);
    }

    private readonly RelayCommand<Window> m_WindowLoadedCommand;
    private readonly RelayCommand<Window> m_WindowClosingCommand;
    private readonly RelayCommand<Window> m_ReloadCommand;
    private readonly RelayCommand<ListView> m_OpenSelectedItemCommand;
    private String m_Manager = "Manager: ";
    private Visibility m_ProgressVisibility = Visibility.Visible;
}