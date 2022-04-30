namespace Narumikazuchi.GroupManager.AddMemberWindow;

public sealed partial class ViewModel
{
    public ViewModel(IActiveDirectoryService activeDirectoryService,
                     IConfiguration configuration,
                     ILocalizationService localizationService,
                     IPreferences preferences)
    {
        m_ActiveDirectoryService = activeDirectoryService;
        m_Configuration = configuration;
        m_LocalizationService = localizationService;
        m_Preferences = preferences;

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

        m_LocalizationService.LocaleChanged += this.UpdateLocalization;
        m_LocalizationService.LocaleListChanged += this.UpdateLocaleList;
    }

    public Rect Rect =>
        m_Preferences.AddUserWindowSize;

    public DirectoryEntry? Principal
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

    public String AddLabel =>
        m_LocalizationService.LocalizationDictionary["Add"];

    public String FilterLabel =>
        m_LocalizationService.LocalizationDictionary["FilterLabel"];

    public String ResultsLabel =>
        m_LocalizationService.LocalizationDictionary["ResultsLabel"];

    public String CancelLabel =>
        m_LocalizationService.LocalizationDictionary["Cancel"];

    public IEnumerable<String> AvailableLanguages =>
        m_LocalizationService.AvailableLocales;

    public String SelectedLocale
    {
        get => m_LocalizationService.SelectedLocale;
        set => m_LocalizationService.SelectedLocale = value;
    }
}

// Non-Public
partial class ViewModel : WindowViewModel
{
    private void UpdateLocalization()
    {
        this.OnPropertyChanged(nameof(this.SelectedLocale));
        this.OnPropertyChanged(nameof(this.AddLabel));
        this.OnPropertyChanged(nameof(this.FilterLabel));
        this.OnPropertyChanged(nameof(this.ResultsLabel));
        this.OnPropertyChanged(nameof(this.CancelLabel));
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

        if (!m_ActiveDirectoryService.TryGetPrincipalByDN(distinguishedName: m_Configuration.PrincipalsDn,
                                                          principal: out DirectoryEntry? principalContainer))
        {
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!m_ActiveDirectoryService.TryGetPrincipalsFilteredBy(groupOrOu: principalContainer,
                                                                 filter: m_FilterParameter,
                                                                 principals: out IEnumerable<DirectoryEntry>? principals))
        {
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        foreach (DirectoryEntry principal in principals)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this.Reset();
                return;
            }

            if (principal.IsUser(m_ActiveDirectoryService))
            {
                Application.Current
                           .Dispatcher
                           .Invoke(() => this.Results
                                             .Add(new UserListItemViewModel(principal)));
            }
            else if (principal.IsGroup(m_ActiveDirectoryService))
            {
                Application.Current
                           .Dispatcher
                           .Invoke(() => this.Results
                                             .Add(new GroupListItemViewModel(principal)));
            }
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

        this.Principal = model.AdsObject;
        this.DisposeItems(model.AdsObject);
        window.DialogResult = true;
        window.Close();
    }

    private void Cancel(Window window)
    {
        this.Principal = null;
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
        m_Preferences.AddUserWindowSize = new()
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