namespace Narumikazuchi.GroupManager.GroupOverviewWindow;

public sealed partial class ViewModel
{
    public ViewModel(IActiveDirectoryService activeDirectoryService,
                     ILocalizationService localizationService,
                     IPreferences preferences,
                     AddMemberWindow.ViewModel addViewModel,
                     AddMemberWindow.Window addWindow)
    {
        m_WindowLoadedCommand = new(this.WindowLoaded);
        m_WindowClosingCommand = new(this.WindowClosing);
        m_ReloadListCommand = new(this.ReloadList);
        m_CancelOperationCommand = new(this.CancelOperation);
        m_AddMemberCommand = new(this.AddMember);
        m_RemoveMemberCommand = new(onExecute: this.RemoveMember,
                                    canExecute: this.CanRemoveMember);
        m_WindowResizedCommand = new(this.WindowResized);

        m_ActiveDirectoryService = activeDirectoryService;
        m_LocalizationService = localizationService;
        m_Preferences = preferences;
        m_AddViewModel = addViewModel;
        m_AddWindow = addWindow;

        this.Title = localizationService.LocalizationDictionary["Unknown"];
        this.GroupName = localizationService.LocalizationDictionary["Unknown"];
        this.StatusText = "StatusLoading";

        m_LocalizationService.LocaleChanged += this.UpdateLocalization;
        m_LocalizationService.LocaleListChanged += this.UpdateLocaleList;
    }

    public void Load(GroupListItemViewModel group)
    {
        ArgumentNullException.ThrowIfNull(group);

        this.Title = group.DisplayName ?? m_LocalizationService.LocalizationDictionary["Unknown"];
        this.GroupName = group.DisplayName ?? m_LocalizationService.LocalizationDictionary["Unknown"];
        m_Principal = group.AdsObject;
    }

    public Rect Rect =>
        m_Preferences.GroupOverviewWindowSize;

    public String Title
    {
        get => String.Format(format: m_LocalizationService.LocalizationDictionary["MembersOf"],
                             arg0: m_Title);
        set
        {
            m_Title = value;
            this.OnPropertyChanged(nameof(this.Title));
        }
    }

    public String GroupName
    {
        get => String.Format(format: m_LocalizationService.LocalizationDictionary["Group"],
                             arg0: m_GroupName);
        set
        {
            m_GroupName = value;
            this.OnPropertyChanged(nameof(this.GroupName));
        }
    }

    public String StatusText
    {
        get => String.Format(format: m_LocalizationService.LocalizationDictionary["Status"],
                             arg0: m_LocalizationService.LocalizationDictionary[m_StatusText]);
        set
        {
            m_StatusText = value;
            this.OnPropertyChanged(nameof(this.StatusText));
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

    public ObservableCollection<AListItemViewModel> Members { get; } = new();

    public ICommand WindowLoadedCommand =>
        m_WindowLoadedCommand;

    public ICommand WindowClosingCommand =>
        m_WindowClosingCommand;

    public ICommand ReloadListCommand =>
        m_ReloadListCommand;

    public ICommand CancelOperationCommand =>
        m_CancelOperationCommand;

    public ICommand AddMemberCommand =>
        m_AddMemberCommand;

    public ICommand RemoveMemberCommand =>
        m_RemoveMemberCommand;

    public ICommand WindowResizedCommand =>
        m_WindowResizedCommand;

    public String AddLabel =>
        m_LocalizationService.LocalizationDictionary["Add"];

    public String RemoveLabel =>
        m_LocalizationService.LocalizationDictionary["Remove"];

    public String ReloadListLabel =>
        m_LocalizationService.LocalizationDictionary["ReloadList"];

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
        this.OnPropertyChanged(nameof(this.Title));
        this.OnPropertyChanged(nameof(this.GroupName));
        this.OnPropertyChanged(nameof(this.StatusText));
        this.OnPropertyChanged(nameof(this.AddLabel));
        this.OnPropertyChanged(nameof(this.RemoveLabel));
        this.OnPropertyChanged(nameof(this.ReloadListLabel));
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

        this.QueryMembers();
    }

    private void WindowClosing(Window window)
    {
        Rect size = new(x: window.Left,
                        y: window.Top,
                        width: window.Width,
                        height: window.Height);
        m_Preferences.GroupOverviewWindowSize = size;
        m_Preferences.Save();

        Application.Current
                   .Dispatcher
                   .Invoke(this.DisposeItems);
    }

    private void ReloadList() =>
        this.QueryMembers();

    private void CancelOperation() =>
        m_TokenSource?.Cancel();

    private void AddMember()
    {
        Boolean? result = m_AddWindow.ShowDialog();

        if (!result.HasValue ||
            !result.Value ||
            m_AddViewModel.Principal is null)
        {
            return;
        }

        if (m_AddViewModel.Principal
                          .IsUser(m_ActiveDirectoryService))
        {
            this.Members
                .Add(new UserListItemViewModel(m_AddViewModel.Principal));
        }
        else if (m_AddViewModel.Principal
                               .IsGroup(m_ActiveDirectoryService))
        {
            this.Members
                .Add(new GroupListItemViewModel(m_AddViewModel.Principal));
        }

        String dn = m_AddViewModel.Principal
                                  .Path
                                  .Remove(0, 7);
        if (!m_ActiveDirectoryService.TryAddPrincipalToGroup(group: m_Principal!,
                                                             distinguishedName: dn))
        {
            MessageBox.Show(messageBoxText: String.Format(format: m_LocalizationService.LocalizationDictionary["FailedToAdd"],
                                                          arg0: dn),
                            caption: "COM Exception",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
        }
    }

    private Boolean CanRemoveMember(ListView view) =>
        view.SelectedItem is not null
                          and AListItemViewModel;

    private void RemoveMember(ListView view)
    {
        if (view.SelectedItem is null ||
            view.SelectedItem is not AListItemViewModel model)
        {
            return;
        }

        String dn = model.AdsObject
                         .Path
                         .Remove(0, 7);
        this.Members
            .Remove(model);
        if (!m_ActiveDirectoryService.TryRemovePrincipalFromGroup(group: m_Principal!,
                                                                  distinguishedName: dn))
        {
            MessageBox.Show(messageBoxText: String.Format(format: m_LocalizationService.LocalizationDictionary["FailedToRemove"],
                                                          arg0: dn),
                            caption: "COM Exception",
                            button: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
        }
    }

    private void QueryMembers()
    {
        if (m_TokenSource is not null)
        {
            m_TokenSource.Cancel();
            m_TokenSource.Dispose();
            m_TokenSource = null;
        }

        this.ProgressVisibility = Visibility.Visible;
        this.StatusText = "StatusLoading";

        m_TokenSource = new();

        Task.Run(() => this.QueryMembers(m_TokenSource.Token))
            .ContinueWith(this.FinishTask);
    }
    private void QueryMembers(CancellationToken cancellationToken = default)
    {
        this.Reset();
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        IReadOnlyList<String> memberDns = m_ActiveDirectoryService.CastPropertyToStringArray(adsObject: m_Principal!,
                                                                                             property: "member");
        foreach (String dn in memberDns)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!DirectoryEntry.Exists($"LDAP://{dn}"))
            {
                continue;
            }

            DirectoryEntry principal = new($"LDAP://{dn}");
            if (principal.IsUser(m_ActiveDirectoryService))
            {
                Application.Current
                           .Dispatcher
                           .Invoke(() => this.Members
                                             .Add(new UserListItemViewModel(principal)));
            }
            else if (principal.IsGroup(m_ActiveDirectoryService))
            {
                Application.Current
                           .Dispatcher
                           .Invoke(() => this.Members
                                             .Add(new GroupListItemViewModel(principal)));
            }
        }
    }

    private void Reset()
    {
        Application.Current
                   .Dispatcher
                   .Invoke(this.DisposeItems);
        Application.Current
                   .Dispatcher
                   .Invoke(this.Members.Clear);
    }

    private void DisposeItems()
    {
        foreach (AListItemViewModel model in this.Members)
        {
            model.Dispose();
        }
    }

    private void FinishTask(Task _)
    {
        this.ProgressVisibility = Visibility.Collapsed;
        this.StatusText = "StatusDone";
        if (m_TokenSource is not null)
        {
            m_TokenSource.Dispose();
            m_TokenSource = null;
        }
    }

    private void WindowResized(Window window)
    {
        m_Preferences.GroupOverviewWindowSize = new()
        {
            X = window.Left,
            Y = window.Top,
            Width = window.Width,
            Height = window.Height
        };
        m_Preferences.Save();
    }

    private readonly IActiveDirectoryService m_ActiveDirectoryService;
    private readonly ILocalizationService m_LocalizationService;
    private readonly IPreferences m_Preferences;
    private readonly AddMemberWindow.ViewModel m_AddViewModel;
    private readonly AddMemberWindow.Window m_AddWindow;
    private readonly RelayCommand<Window> m_WindowLoadedCommand;
    private readonly RelayCommand<Window> m_WindowClosingCommand;
    private readonly RelayCommand m_ReloadListCommand;
    private readonly RelayCommand m_CancelOperationCommand;
    private readonly RelayCommand m_AddMemberCommand;
    private readonly RelayCommand<ListView> m_RemoveMemberCommand;
    private readonly RelayCommand<Window> m_WindowResizedCommand;
    private Visibility m_ProgressVisibility = Visibility.Visible;
    private String m_Title = String.Empty;
    private String m_GroupName = String.Empty;
    private String m_StatusText = String.Empty;
    private DirectoryEntry? m_Principal;
    private CancellationTokenSource? m_TokenSource;
}