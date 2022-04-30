namespace Narumikazuchi.GroupManager.MainWindow;

public sealed partial class ViewModel
{
    public ViewModel(IActiveDirectoryService activeDirectoryService,
                     IConfiguration configuration,
                     ILocalizationService localizationService,
                     IPreferences preferences,
                     GroupOverviewWindow.ViewModel groupViewModel,
                     GroupOverviewWindow.Window groupWindow)
    {
        m_WindowLoadedCommand = new(this.WindowLoaded);
        m_WindowClosingCommand = new(this.WindowClosing);
        m_ReloadListCommand = new(this.ReloadList);
        m_CancelOperationCommand = new(this.CancelOperation);
        m_OpenSelectedItemCommand = new(onExecute: this.OpenSelectedItem,
                                        canExecute: this.CanOpenSelectedItem);
        m_WindowResizedCommand = new(this.WindowResized);

        m_ActiveDirectoryService = activeDirectoryService;
        m_Configuration = configuration;
        m_LocalizationService = localizationService;
        m_Preferences = preferences;
        m_GroupViewModel = groupViewModel;
        m_GroupWindow = groupWindow;

        this.Manager = m_LocalizationService.LocalizationDictionary["Unknown"];

        m_LocalizationService.LocaleChanged += this.UpdateLocalization;
        m_LocalizationService.LocaleListChanged += this.UpdateLocaleList;
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

    public Rect Rect =>
        m_Preferences.MainWindowSize;

    public String Manager
    {
        get => String.Format(format: m_LocalizationService.LocalizationDictionary["Manager"],
                             arg0: m_Manager);
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
        m_ReloadListCommand;

    public ICommand CancelOperationCommand =>
        m_CancelOperationCommand;

    public ICommand OpenSelectedItemCommand =>
        m_OpenSelectedItemCommand;

    public ICommand WindowResizedCommand =>
        m_WindowResizedCommand;

    public String Title =>
        m_LocalizationService.LocalizationDictionary["MainTitle"];

    public String ReloadListLabel =>
        m_LocalizationService.LocalizationDictionary["ReloadList"];

    public String ShowMembersLabel =>
        m_LocalizationService.LocalizationDictionary["ShowMembers"];

    public String CancelLabel =>
        m_LocalizationService.LocalizationDictionary["Cancel"];

    public String CloseLabel =>
        m_LocalizationService.LocalizationDictionary["Close"];

    public IEnumerable<String> AvailableLanguages =>
        m_LocalizationService.AvailableLocales;

    public String SelectedLocale
    {
        get => m_LocalizationService.SelectedLocale;
        set => m_LocalizationService.SelectedLocale = value;
    }
}

partial class ViewModel : WindowViewModel
{
    private void UpdateLocalization()
    {
        this.OnPropertyChanged(nameof(this.SelectedLocale));
        this.OnPropertyChanged(nameof(this.Manager));
        this.OnPropertyChanged(nameof(this.Title));
        this.OnPropertyChanged(nameof(this.ReloadListLabel));
        this.OnPropertyChanged(nameof(this.ShowMembersLabel));
        this.OnPropertyChanged(nameof(this.CancelLabel));
        this.OnPropertyChanged(nameof(this.CloseLabel));
    }

    private void UpdateLocaleList() =>
        this.OnPropertyChanged(nameof(this.AvailableLanguages));

    private void WindowLoaded(Window window)
    {
        if (window.Top + window.Height / 2 > SystemParameters.VirtualScreenHeight)
        {
            window.Top = SystemParameters.VirtualScreenHeight - window.Height;
        }
        if (window.Top < 0)
        {
            window.Top = 0;
        }

        if (window.Left + window.Width / 2 > SystemParameters.VirtualScreenWidth)
        {
            window.Left = SystemParameters.VirtualScreenWidth - window.Width;
        }
        if (window.Left < 0)
        {
            window.Left = 0;
        }

        this.RequeryManagedGroups();
    }

    private void WindowClosing(Window window)
    {
        Rect size = new(x: window.Left,
                        y: window.Top,
                        width: window.Width,
                        height: window.Height);
        m_Preferences.MainWindowSize = size;
        m_Preferences.Save();

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

        m_GroupViewModel.Load(group);
        m_GroupWindow.ShowDialog();
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
        if (user.ContextType is not ContextType.Domain)
        {
            MessageBox.Show(messageBoxText: m_LocalizationService.LocalizationDictionary["UserIsNotPartOfDomain"],
                            caption: "Not part of domain",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        this.Manager = user.DisplayName ?? user.Name;
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        IReadOnlyList<DirectoryEntry> groups = GetGroups(user.DistinguishedName);
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

    private IReadOnlyList<DirectoryEntry> GetGroups(String dn)
    {
        if (!m_ActiveDirectoryService.TryGetPrincipalByDN(distinguishedName: dn,
                                                          out DirectoryEntry? user))
        {
            MessageBox.Show(messageBoxText: String.Format(format: m_LocalizationService.LocalizationDictionary["FailedToFindObject"],
                                                          arg0: dn),
                            caption: "Failed to find object",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
            return Array.Empty<DirectoryEntry>();
        }

        if (!m_ActiveDirectoryService.TryGetGroupsManagedByUser(user: user,
                                                                groups: out IEnumerable<DirectoryEntry>? groups))
        {
            MessageBox.Show(messageBoxText: String.Format(format: m_LocalizationService.LocalizationDictionary["FailedToFindAdsObject"],
                                                          arg0: m_Configuration.ManagedGroupsDn),
                            caption: "Failed to open OU",
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
        m_Preferences.MainWindowSize = new()
        {
            X = window.Left,
            Y = window.Top,
            Width = window.Width,
            Height = window.Height
        };
        m_Preferences.Save();
    }

    private readonly IActiveDirectoryService m_ActiveDirectoryService;
    private readonly IConfiguration m_Configuration;
    private readonly ILocalizationService m_LocalizationService;
    private readonly IPreferences m_Preferences;
    private readonly GroupOverviewWindow.Window m_GroupWindow;
    private readonly GroupOverviewWindow.ViewModel m_GroupViewModel;
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