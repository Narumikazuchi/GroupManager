namespace Narumikazuchi.GroupManager.GroupOverviewWindow;

public sealed partial class ViewModel
{
    public ViewModel()
    {
        m_WindowLoadedCommand = new(this.WindowLoaded);
        m_WindowClosingCommand = new(this.WindowClosing);
        m_ReloadListCommand = new(this.ReloadList);
        m_AddMemberCommand = new(this.AddMember);
        m_RemoveMemberCommand = new(this.RemoveMember);
    }

    public void Load(GroupListItemViewModel group!!)
    {
        this.Title = group.DisplayName ?? "unbekannt";
        this.GroupName = group.DisplayName ?? "unbekannt";
        m_AdsObject = group.AdsObject;
    }

    public String Title
    {
        get => m_Title;
        set
        {
            m_Title = $"Mitglieder von {value}";
            this.OnPropertyChanged(nameof(this.Title));
        }
    }

    public String GroupName
    {
        get => m_GroupName;
        set
        {
            m_GroupName = $"Gruppe: {value}";
            this.OnPropertyChanged(nameof(this.GroupName));
        }
    }

    public String StatusText
    {
        get => m_StatusText;
        set
        {
            m_StatusText = $"Status: {value}";
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

    public ICommand AddMemberCommand =>
        m_AddMemberCommand;

    public ICommand RemoveMemberCommand =>
        m_RemoveMemberCommand;
}

partial class ViewModel : WindowViewModel
{
    private void WindowLoaded(Window window)
    {
        Preferences preferences = Preferences.Current;
        window.Width = preferences.GroupOverviewWindowSize.Width;
        window.Height = preferences.GroupOverviewWindowSize.Height;

        Double left = preferences.GroupOverviewWindowSize.X;
        Double top = preferences.GroupOverviewWindowSize.Y;
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

    private void AddMember()
    {
        AddUserWindow.Window window = new();
        AddUserWindow.ViewModel viewModel = (AddUserWindow.ViewModel)window.DataContext;
        Boolean? result = window.ShowDialog();

        if (!result.HasValue ||
            !result.Value)
        {
            return;
        }

        IReadOnlyList<String> classes = ActiveDirectoryInterface.CastPropertyToStringArray(adsObject: viewModel.AdsObject,
                                                                                           property: "objectClass");
        foreach (String objClass in classes)
        {
            if (objClass == "user")
            {
                this.Members
                    .Add(new UserListItemViewModel(viewModel.AdsObject));
            }
            else if (objClass == "group")
            {
                this.Members
                    .Add(new GroupListItemViewModel(viewModel.AdsObject));
            }
        }

        String dn = viewModel.AdsObject
                             .Path
                             .Remove(0, 7);
        if (!ActiveDirectoryInterface.TryAddObjectToGroup(group: m_AdsObject!,
                                                          objectDn: dn))
        {
            MessageBox.Show($"Konnte das Object '{dn}' nicht zur Gruppe hinzufügen!", "COM Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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
        if (!ActiveDirectoryInterface.TryRemoveObjectFromGroup(group: m_AdsObject!,
                                                               objectDn: dn))
        {
            MessageBox.Show($"Konnte das Mitglied '{dn}' nicht von der Gruppe entfernen!", "COM Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DisposeItems()
    {
        foreach (AListItemViewModel model in this.Members)
        {
            model.Dispose();
        }
    }

    private void QueryMembers() => 
        Task.Run(this.StartLoad)
            .ContinueWith(this.QueryMembers)
            .ContinueWith(this.FinishLoad);
    private void QueryMembers(Task _)
    {
        Application.Current
                   .Dispatcher
                   .Invoke(this.DisposeItems);
        Application.Current
                   .Dispatcher
                   .Invoke(this.Members.Clear);
        IReadOnlyList<String> memberDns = ActiveDirectoryInterface.CastPropertyToStringArray(adsObject: m_AdsObject!,
                                                                                             property: "member");
        foreach (String dn in memberDns)
        {
            if (!DirectoryEntry.Exists($"LDAP://{dn}"))
            {
                continue;
            }

            DirectoryEntry adsObject = new($"LDAP://{dn}");
            IReadOnlyList<String> classes = ActiveDirectoryInterface.CastPropertyToStringArray(adsObject: adsObject,
                                                                                               property: "objectClass");
            foreach (String objClass in classes)
            {
                if (objClass == "user")
                {
                    this.Members
                        .Add(new UserListItemViewModel(adsObject));
                }
                else if (objClass == "group")
                {
                    this.Members
                        .Add(new GroupListItemViewModel(adsObject));
                }
            }
        }
    }

    private void StartLoad()
    {
        this.ProgressVisibility = Visibility.Visible;
        this.StatusText = "lädt...";
    }

    private void FinishLoad(Task _)
    {
        this.ProgressVisibility = Visibility.Collapsed;
        this.StatusText = "fertig";
    }

    private readonly RelayCommand<Window> m_WindowLoadedCommand;
    private readonly RelayCommand<Window> m_WindowClosingCommand;
    private readonly RelayCommand m_ReloadListCommand;
    private readonly RelayCommand m_AddMemberCommand;
    private readonly RelayCommand<ListView> m_RemoveMemberCommand;
    private String m_Title = "Mitglieder von unbekannt";
    private String m_GroupName = "Gruppe: unbekannt";
    private String m_StatusText = "Status: lädt...";
    private Visibility m_ProgressVisibility = Visibility.Visible;
    private DirectoryEntry? m_AdsObject;
}