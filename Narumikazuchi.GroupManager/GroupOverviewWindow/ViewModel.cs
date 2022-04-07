namespace Narumikazuchi.GroupManager.GroupOverviewWindow;

public sealed partial class ViewModel
{
    public ViewModel()
    {
        m_WindowLoadedCommand = new(this.WindowLoaded);
        m_WindowClosingCommand = new(this.WindowClosing);
        m_ReloadListCommand = new(this.ReloadList);
        m_CancelOperationCommand = new(this.CancelOperation);
        m_AddMemberCommand = new(this.AddMember);
        m_RemoveMemberCommand = new(onExecute: this.RemoveMember,
                                    canExecute: this.CanRemoveMember);
        m_WindowResizedCommand = new(this.WindowResized);

        this.Title = Localization.Instance.Unknown;
        this.GroupName = Localization.Instance.Unknown;
        this.StatusText = Localization.Instance.StatusLoading;
    }

    public void Load(GroupListItemViewModel group!!)
    {
        this.Title = group.DisplayName ?? Localization.Instance.Unknown;
        this.GroupName = group.DisplayName ?? Localization.Instance.Unknown;
        m_Principal = group.AdsObject;
    }

    public String Title
    {
        get => m_Title;
        set
        {
            m_Title = String.Format(format: Localization.Instance.MembersOf,
                                    arg0: value);
            this.OnPropertyChanged(nameof(this.Title));
        }
    }

    public String GroupName
    {
        get => m_GroupName;
        set
        {
            m_GroupName = String.Format(format: Localization.Instance.Group,
                                        arg0: value);
            this.OnPropertyChanged(nameof(this.GroupName));
        }
    }

    public String StatusText
    {
        get => m_StatusText;
        set
        {
            m_StatusText = String.Format(format: Localization.Instance.Status,
                                         arg0: value);
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
}

partial class ViewModel : WindowViewModel
{
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
        Preferences preferences = Preferences.Current;
        Rect size = new(x: window.Left,
                        y: window.Top,
                        width: window.Width,
                        height: window.Height);
        preferences.GroupOverviewWindowSize = size;
        preferences.Save();

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
        AddMemberWindow.Window window = new();
        AddMemberWindow.ViewModel viewModel = (AddMemberWindow.ViewModel)window.DataContext;
        Boolean? result = window.ShowDialog();

        if (!result.HasValue ||
            !result.Value ||
            viewModel.Principal is null)
        {
            return;
        }

        if (viewModel.Principal
                     .IsUser())
        {
            this.Members
                .Add(new UserListItemViewModel(viewModel.Principal));
        }
        else if (viewModel.Principal
                          .IsGroup())
        {
            this.Members
                .Add(new GroupListItemViewModel(viewModel.Principal));
        }

        String dn = viewModel.Principal
                             .Path
                             .Remove(0, 7);
        if (!ActiveDirectoryInterface.TryAddPrincipalToGroup(group: m_Principal!,
                                                             objectDn: dn))
        {
            MessageBox.Show(messageBoxText: String.Format(format: Localization.Instance.FailedToAdd,
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
        if (!ActiveDirectoryInterface.TryRemovePrincipalFromGroup(group: m_Principal!,
                                                               objectDn: dn))
        {
            MessageBox.Show(messageBoxText: String.Format(format: Localization.Instance.FailedToRemove,
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
        this.StatusText = Localization.Instance.StatusLoading;

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
        IReadOnlyList<String> memberDns = ActiveDirectoryInterface.CastPropertyToStringArray(adsObject: m_Principal!,
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
            if (principal.IsUser())
            {
                Application.Current
                           .Dispatcher
                           .Invoke(() => this.Members
                                             .Add(new UserListItemViewModel(principal)));
            }
            else if (principal.IsGroup())
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
        this.StatusText = Localization.Instance.StatusDone;
        if (m_TokenSource is not null)
        {
            m_TokenSource.Dispose();
            m_TokenSource = null;
        }
    }

    private void WindowResized(Window window)
    {
        Preferences preferences = Preferences.Current;
        preferences.GroupOverviewWindowSize = new()
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