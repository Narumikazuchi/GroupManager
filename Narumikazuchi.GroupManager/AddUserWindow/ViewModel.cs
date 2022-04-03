namespace Narumikazuchi.GroupManager.AddUserWindow;

public sealed partial class ViewModel
{
    public ViewModel()
    {
        m_WindowLoadedCommand = new(this.WindowLoaded);
        m_StartFilteringCommand = new(this.StartFiltering);
        m_CancelOperationCommand = new(this.CancelOperation);
        m_AddObjectCommand = new(this.AddObject);
        m_CancelCommand = new(this.Cancel);
        m_WindowResizedCommand = new(this.WindowResized);

        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.Results);
        view.SortDescriptions
            .Add(new(propertyName: "DisplayName",
                     direction: ListSortDirection.Ascending));
    }

    public DirectoryEntry? AdsObject
    {
        get;
        set;
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

    public ObservableCollection<AListItemViewModel> Results { get; } = new();

    public ICommand WindowLoadedCommand =>
        m_WindowLoadedCommand;

    public ICommand StartFilteringCommand =>
        m_StartFilteringCommand;

    public ICommand CancelOperationCommand =>
        m_CancelOperationCommand;

    public ICommand AddObjectCommand =>
        m_AddObjectCommand;

    public ICommand CancelCommand =>
        m_CancelCommand;

    public ICommand WindowResizedCommand =>
        m_WindowResizedCommand;
}

// Non-Public
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
    }

    private void StartFiltering(TextBox textBox)
    {
        if (m_TokenSource is not null)
        {
            m_TokenSource.Cancel();
            m_TokenSource.Dispose();
            m_TokenSource = null;
        }

        m_FilterParameter = textBox.Text;
        if (String.IsNullOrEmpty(m_FilterParameter))
        {
            this.Reset();
            this.FinishTask(default);
            return;
        }

        this.ProgressVisibility = Visibility.Visible;

        m_TokenSource = new();

        Task.Run(() => this.QueryResults(m_TokenSource.Token))
            .ContinueWith(this.FinishTask);
    }

    private void CancelOperation() =>
        m_TokenSource?.Cancel();

    private void QueryResults(CancellationToken cancellationToken = default)
    {
        this.Reset();
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        Configuration configuration = Configuration.Current;

        // Users
        if (!ActiveDirectoryInterface.TryGetOU(dn: configuration.UserOuDn,
                                               ou: out DirectoryEntry? userOu))
        {
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!ActiveDirectoryInterface.TryGetObjectsFilteredBy(ou: userOu,
                                                              filter: m_FilterParameter,
                                                              findGroups: false,
                                                              adsObjects: out IEnumerable<DirectoryEntry>? users))
        {
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        foreach (DirectoryEntry user in users)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this.Reset();
                return;
            }
            Application.Current
                       .Dispatcher
                       .Invoke(() => this.Results
                                         .Add(new UserListItemViewModel(user)));
        }

        // Groups
        if (!ActiveDirectoryInterface.TryGetOU(dn: configuration.GroupOuDn,
                                               ou: out DirectoryEntry? groupOu))
        {
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            this.Reset();
            return;
        }

        if (!ActiveDirectoryInterface.TryGetObjectsFilteredBy(ou: groupOu,
                                                              filter: m_FilterParameter,
                                                              findGroups: true,
                                                              adsObjects: out IEnumerable<DirectoryEntry>? groups))
        {
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            this.Reset();
            return;
        }

        foreach (DirectoryEntry group in groups)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this.Reset();
                return;
            }
            Application.Current
                       .Dispatcher
                       .Invoke(() => this.Results
                                         .Add(new GroupListItemViewModel(group)));
        }
    }

    private void FinishTask(Task? _)
    {
        this.ProgressVisibility = Visibility.Collapsed;
        if (m_TokenSource is not null)
        {
            m_TokenSource.Dispose();
            m_TokenSource = null;
        }
    }

    private void AddObject(Window window)
    {
        if (window.m_Results
                  .SelectedItem is null ||
            window.m_Results
                  .SelectedItem is not AListItemViewModel model)
        {
            return;
        }

        this.AdsObject = model.AdsObject;
        this.DisposeItems(model.AdsObject);
        window.DialogResult = true;
        window.Close();
    }

    private void Cancel(Window window)
    {
        this.AdsObject = null;
        this.DisposeItems();
        window.DialogResult = false;
        window.Close();
    }

    private void Reset()
    {
        Application.Current
                   .Dispatcher
                   .Invoke(this.DisposeItems);
        Application.Current
                   .Dispatcher
                   .Invoke(this.Results.Clear);
    }

    private void DisposeItems() =>
        this.DisposeItems(null);
    private void DisposeItems(DirectoryEntry? excluded)
    {
        foreach (AListItemViewModel model in this.Results)
        {
            if (excluded is not null &&
                excluded.Guid == model.AdsObject
                                      .Guid)
            {
                continue;
            }
            model.Dispose();
        }
    }

    private void WindowResized(Window window)
    {
        Preferences preferences = Preferences.Current;
        preferences.AddUserWindowSize = new()
        {
            X = window.Left,
            Y = window.Top,
            Width = window.Width,
            Height = window.Height
        };
        preferences.Save();
    }

    private readonly RelayCommand<Window> m_WindowLoadedCommand;
    private readonly RelayCommand<TextBox> m_StartFilteringCommand;
    private readonly RelayCommand m_CancelOperationCommand;
    private readonly RelayCommand<Window> m_AddObjectCommand;
    private readonly RelayCommand<Window> m_CancelCommand;
    private readonly RelayCommand<Window> m_WindowResizedCommand;
    private String m_FilterParameter = String.Empty;
    private Visibility m_ProgressVisibility = Visibility.Collapsed;
    private CancellationTokenSource? m_TokenSource;
}