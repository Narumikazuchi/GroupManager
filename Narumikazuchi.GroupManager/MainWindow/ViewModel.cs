namespace Narumikazuchi.GroupManager.MainWindow;

public sealed partial class ViewModel
{
    public ViewModel()
    {
        m_WindowLoadedCommand = new(this.WindowLoaded);
        m_WindowClosingCommand = new(this.WindowClosing);
        m_ReloadListCommand = new(this.ReloadList);
        m_CancelOperationCommand = new(this.CancelOperation);
        m_OpenSelectedItemCommand = new(onExecute: this.OpenSelectedItem,
                                        canExecute: this.CanOpenSelectedItem);
        m_WindowResizedCommand = new(this.WindowResized);

        this.Manager = Localization.Instance.Unknown;
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
            m_Manager = String.Format(format: Localization.Instance.Manager,
                                      arg0: value);
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
        m_ReloadListCommand;

    public ICommand CancelOperationCommand =>
        m_CancelOperationCommand;

    public ICommand OpenSelectedItemCommand =>
        m_OpenSelectedItemCommand;

    public ICommand WindowResizedCommand =>
        m_WindowResizedCommand;
}

partial class ViewModel : WindowViewModel
{
    private void WindowLoaded(Window window)
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

        this.RequeryManagedGroups();
    }

    private void WindowClosing(Window window)
    {
        Preferences preferences = Preferences.Current;
        Rect size = new(x: window.Left,
                        y: window.Top,
                        width: window.Width,
                        height: window.Height);
        preferences.MainWindowSize = size;
        preferences.Save();

        this.Reset();
    }

    private void ReloadList() => 
        this.RequeryManagedGroups();

    private void CancelOperation() =>
        m_TokenSource?.Cancel();

    private Boolean CanOpenSelectedItem(ListView view) =>
        view.SelectedItem is not null 
                          and GroupListItemViewModel;

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

    private void RequeryManagedGroups()
    {
        if (m_TokenSource is not null)
        {
            m_TokenSource.Cancel();
            m_TokenSource.Dispose();
            m_TokenSource = null;
        }

        this.ProgressVisibility = Visibility.Visible;

        m_TokenSource = new();

        Task.Run(() => this.QueryManagedGroups(m_TokenSource.Token))
            .ContinueWith(this.FinishTask);
    }

    private void QueryManagedGroups(CancellationToken cancellationToken = default)
    {
        this.Reset();
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        UserPrincipal user = UserPrincipal.Current;
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        this.Manager = user.DisplayName ?? user.Name;
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        IReadOnlyList<DirectoryEntry> groups = GetGroups(user.SamAccountName);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        foreach (DirectoryEntry entry in groups)
        {
            Application.Current
                        .Dispatcher
                        .Invoke(() => this.ManagedGroups
                                            .Add(new GroupListItemViewModel(entry)));
            if (cancellationToken.IsCancellationRequested)
            {
                this.Reset();
                return;
            }
        }
    }

    private static IReadOnlyList<DirectoryEntry> GetGroups(String samAccountName)
    {
        Configuration configuration = Configuration.Current;
        if (!ActiveDirectoryInterface.TryGetOU(dn: configuration.UserOuDn,
                                               ou: out DirectoryEntry? userOu))
        {
            MessageBox.Show(messageBoxText: String.Format(format: Localization.Instance
                                                                              .FailedToOpenOU,
                                                          arg0: configuration.UserOuDn),
                            caption: "Failed to open OU",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }
        if (!ActiveDirectoryInterface.TryGetUserBySAMAccountName(ou: userOu,
                                                                 samAccountName: samAccountName,
                                                                 out DirectoryEntry? user))
        {
            MessageBox.Show(messageBoxText: String.Format(format: Localization.Instance
                                                                              .FailedToFindObject,
                                                          arg0: samAccountName),
                            caption: "Failed to find object",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }
        if (!ActiveDirectoryInterface.TryGetOU(dn: configuration.GroupOuDn,
                                               ou: out DirectoryEntry? groupOu))
        {
            MessageBox.Show(messageBoxText: String.Format(format: Localization.Instance
                                                                              .FailedToOpenOU,
                                                          arg0: configuration.GroupOuDn),
                            caption: "Failed to open OU",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }
        if (!ActiveDirectoryInterface.TryGetGroupsManagedByUser(ou: groupOu,
                                                                user: user,
                                                                groups: out IEnumerable<DirectoryEntry>? groups))
        {
            MessageBox.Show(messageBoxText: Localization.Instance.NoObjectsFound,
                            caption: "No objects found",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }
        return new List<DirectoryEntry>(collection: groups);
    }

    private void Reset()
    {
        Application.Current
                   .Dispatcher
                   .Invoke(this.DisposeItems);
        Application.Current
                   .Dispatcher
                   .Invoke(this.ManagedGroups.Clear);
    }

    private void DisposeItems()
    {
        foreach (AListItemViewModel model in this.ManagedGroups)
        {
            model.Dispose();
        }
    }

    private void FinishTask(Task _)
    {
        this.ProgressVisibility = Visibility.Collapsed;
        if (m_TokenSource is not null)
        {
            m_TokenSource.Dispose();
            m_TokenSource = null;
        }
    }

    private void WindowResized(Window window)
    {
        Preferences preferences = Preferences.Current;
        preferences.MainWindowSize = new()
        {
            X = window.Left,
            Y = window.Top,
            Width = window.Width,
            Height = window.Height
        };
        preferences.Save();
    }

    private readonly RelayCommand<Window> m_WindowLoadedCommand;
    private readonly RelayCommand<Window> m_WindowClosingCommand;
    private readonly RelayCommand m_ReloadListCommand;
    private readonly RelayCommand m_CancelOperationCommand;
    private readonly RelayCommand<ListView> m_OpenSelectedItemCommand;
    private readonly RelayCommand<Window> m_WindowResizedCommand;
    private Visibility m_ProgressVisibility = Visibility.Visible;
    private String m_Manager = String.Empty;
    private CancellationTokenSource? m_TokenSource;
}