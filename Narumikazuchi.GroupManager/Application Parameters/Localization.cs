using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

public sealed partial class Localization
{
    static Localization()
    {
        s_Languages = new(LoadLanguages);
    }

    public static Boolean TryLoad()
    {
        String path = Path.Combine(Environment.CurrentDirectory,
                                   $"{Instance.Locale}.locale");
        FileInfo file = new(path);

        if (!file.Exists)
        {
            return false;
        }
        
        using FileStream stream = new(path: file.FullName,
                                      mode: FileMode.Open,
                                      access: FileAccess.Read,
                                      share: FileShare.Read);
        using XmlReader reader = XmlReader.Create(input: stream);

        while (reader.Read())
        {
            String? key;
            String? value;
            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "text")
            {
                key = reader.GetAttribute("key");
                if (key is null)
                {
                    return false;
                }
                value = reader.GetAttribute("value");
                if (value is null)
                {
                    return false;
                }
                switch (key)
                {
                    case nameof(MainTitle):
                        Instance.m_MainTitle = value;
                        break;
                    case nameof(ReloadList):
                        Instance.m_ReloadList = value;
                        break;
                    case nameof(ShowMembers):
                        Instance.m_ShowMembers = value;
                        break;
                    case nameof(Close):
                        Instance.m_Close = value;
                        break;
                    case nameof(Add):
                        Instance.m_Add = value;
                        break;
                    case nameof(Remove):
                        Instance.m_Remove = value;
                        break;
                    case nameof(Cancel):
                        Instance.m_Cancel = value;
                        break;
                    case nameof(FilterLabel):
                        Instance.m_FilterLabel = value;
                        break;
                    case nameof(ResultsLabel):
                        Instance.m_ResultsLabel = value;
                        break;
                    case nameof(Configuration):
                        Instance.m_Configuration = value;
                        break;
                    case nameof(DefaultLocale):
                        Instance.m_DefaultLocale = value;
                        break;
                    case nameof(UseGroup):
                        Instance.m_UseGroup = value;
                        break;
                    case nameof(PrincipalOuDn):
                        Instance.m_PrincipalOuDn = value;
                        break;
                    case nameof(GroupOuDn):
                        Instance.m_GroupOuDn = value;
                        break;
                    case nameof(PrincipalGroupDn):
                        Instance.m_PrincipalGroupDn = value;
                        break;
                    case nameof(GroupsGroupDn):
                        Instance.m_GroupsGroupDn = value;
                        break;
                    case nameof(Save):
                        Instance.m_Save = value;
                        break;
                    case nameof(Manager):
                        Instance.m_Manager = value;
                        break;
                    case nameof(Unknown):
                        Instance.m_Unknown = value;
                        break;
                    case nameof(MembersOf):
                        Instance.m_MembersOf = value;
                        break;
                    case nameof(Group):
                        Instance.m_Group = value;
                        break;
                    case nameof(Status):
                        Instance.m_Status = value;
                        break;
                    case nameof(StatusLoading):
                        Instance.m_StatusLoading = value;
                        break;
                    case nameof(StatusDone):
                        Instance.m_StatusDone = value;
                        break;
                    case nameof(UserIsNotPartOfDomain):
                        Instance.m_UserIsNotPartOfDomain = value;
                        break;
                    case nameof(FailedToFindAdsObject):
                        Instance.m_FailedToFindAdsObject = value;
                        break;
                    case nameof(FailedToAdd):
                        Instance.m_FailedToAdd = value;
                        break;
                    case nameof(FailedToRemove):
                        Instance.m_FailedToRemove = value;
                        break;
                    case nameof(FailedToFindObject):
                        Instance.m_FailedToFindObject = value;
                        break;
                    case nameof(NoObjectsFound):
                        Instance.m_NoObjectsFound = value;
                        break;
                }
                continue;
            }
        }
        return true;
    }

    public String Locale
    {
        get => m_Locale;
        set
        {
            m_Locale = value;
            if (TryLoad())
            {
                this.OnPropertyChanged(nameof(this.MainTitle));
                this.OnPropertyChanged(nameof(this.ReloadList));
                this.OnPropertyChanged(nameof(this.ShowMembers));
                this.OnPropertyChanged(nameof(this.Close));
                this.OnPropertyChanged(nameof(this.Add));
                this.OnPropertyChanged(nameof(this.Remove));
                this.OnPropertyChanged(nameof(this.Cancel));
                this.OnPropertyChanged(nameof(this.FilterLabel));
                this.OnPropertyChanged(nameof(this.ResultsLabel));
                this.OnPropertyChanged(nameof(this.Configuration));
                this.OnPropertyChanged(nameof(this.DefaultLocale));
                this.OnPropertyChanged(nameof(this.UseGroup));
                this.OnPropertyChanged(nameof(this.PrincipalOuDn));
                this.OnPropertyChanged(nameof(this.GroupOuDn));
                this.OnPropertyChanged(nameof(this.PrincipalGroupDn));
                this.OnPropertyChanged(nameof(this.GroupsGroupDn));
                this.OnPropertyChanged(nameof(this.Save));
                this.OnPropertyChanged(nameof(this.Manager));
                this.OnPropertyChanged(nameof(this.Unknown));
                this.OnPropertyChanged(nameof(this.MembersOf));
                this.OnPropertyChanged(nameof(this.Group));
                this.OnPropertyChanged(nameof(this.Status));
                this.OnPropertyChanged(nameof(this.StatusLoading));
                this.OnPropertyChanged(nameof(this.StatusDone));
                this.OnPropertyChanged(nameof(this.UserIsNotPartOfDomain));
                this.OnPropertyChanged(nameof(this.FailedToFindAdsObject));
                this.OnPropertyChanged(nameof(this.FailedToAdd));
                this.OnPropertyChanged(nameof(this.FailedToRemove));
                this.OnPropertyChanged(nameof(this.FailedToFindObject));
                this.OnPropertyChanged(nameof(this.NoObjectsFound));
                if (m_Locale != GroupManager.Configuration.Current
                                                          .DefaultLocale)
                {
                    Preferences.Current
                               .Locale = m_Locale;
                    Preferences.Current
                               .Save();
                }
                if (AvailableLanguages[this.SelectedLocaleIndex] != m_Locale)
                {
                    m_LocaleIndex = ((List<String>)AvailableLanguages).IndexOf(m_Locale);
                    this.OnPropertyChanged(nameof(this.SelectedLocaleIndex));
                }
            }
        }
    }

    public Int32 SelectedLocaleIndex
    {
        get => m_LocaleIndex;
        set
        {
            m_LocaleIndex = value;
            this.Locale = AvailableLanguages[value];
            this.OnPropertyChanged(nameof(this.SelectedLocaleIndex));
        }
    }

    public static IReadOnlyList<String> AvailableLanguages =>
        s_Languages.Value;

    public static Localization Instance { get; } = new();

    public String MainTitle =>
        m_MainTitle;

    public String ReloadList =>
        m_ReloadList;

    public String ShowMembers =>
        m_ShowMembers;

    public String Close =>
        m_Close;

    public String Add =>
        m_Add;

    public String Remove =>
        m_Remove;

    public String Cancel =>
        m_Cancel;

    public String FilterLabel =>
        m_FilterLabel;

    public String ResultsLabel =>
        m_ResultsLabel;

    public String Configuration =>
        m_Configuration;

    public String DefaultLocale =>
        m_DefaultLocale;

    public String UseGroup =>
        m_UseGroup;

    public String PrincipalOuDn =>
        m_PrincipalOuDn;

    public String GroupOuDn =>
        m_GroupOuDn;

    public String PrincipalGroupDn =>
        m_PrincipalGroupDn;

    public String GroupsGroupDn =>
        m_GroupsGroupDn;

    public String Save =>
        m_Save;

    public String Manager =>
        m_Manager;

    public String Unknown =>
        m_Unknown;

    public String MembersOf =>
        m_MembersOf;

    public String Group =>
        m_Group;

    public String Status =>
        m_Status;

    public String StatusLoading =>
        m_StatusLoading;

    public String StatusDone =>
        m_StatusDone;

    public String UserIsNotPartOfDomain =>
        m_UserIsNotPartOfDomain;

    public String FailedToFindAdsObject =>
        m_FailedToFindAdsObject;

    public String FailedToAdd =>
        m_FailedToAdd;

    public String FailedToRemove =>
        m_FailedToRemove;

    public String FailedToFindObject =>
        m_FailedToFindObject;

    public String NoObjectsFound =>
        m_NoObjectsFound;
}

// Non-Public
partial class Localization
{
    private Localization()
    { }

    private static IReadOnlyList<String> LoadLanguages()
    {
        List<String> languages = new();
        DirectoryInfo directory = new(Environment.CurrentDirectory);
        languages.AddRange(directory.EnumerateFiles("*.locale")
                                    .Select(x => x.Name.Replace(oldValue: ".locale",
                                                                newValue: ""))
                                    .Prepend("en"));
        return languages;
    }

    private static readonly Lazy<IReadOnlyList<String>> s_Languages;
    private String m_Locale = "en";
    private Int32 m_LocaleIndex = 0;
    private String m_MainTitle = "Group Manager";
    private String m_ReloadList = "Reload list";
    private String m_ShowMembers = "Show members";
    private String m_Close = "Close";
    private String m_Add = "Add";
    private String m_Remove = "Remove";
    private String m_Cancel = "Cancel";
    private String m_FilterLabel = "Filter by:";
    private String m_ResultsLabel = "Results:";
    private String m_Configuration = "Configuration";
    private String m_DefaultLocale = "Default language:";
    private String m_UseGroup = "Use Security Group";
    private String m_PrincipalOuDn = "Distinguished Name of the Organisational Unit containing the accessible principals:";
    private String m_GroupOuDn = "Distinguished Name of the Organisational Unit containing the managed groups:";
    private String m_PrincipalGroupDn = "Distinguished Name of the Security Group that contains the accessible principals:";
    private String m_GroupsGroupDn = "Distinguished Name of the Security Group that contains the managed groups:";
    private String m_Save = "Save";
    private String m_Manager = "Manager: {0}";
    private String m_Unknown = "unknown";
    private String m_MembersOf = "Members of {0}";
    private String m_Group = "Group: {0}";
    private String m_Status = "Status: {0}";
    private String m_StatusLoading = "loading...";
    private String m_StatusDone = "done";
    private String m_UserIsNotPartOfDomain = "The currently logged in user is not part of a domain.";
    private String m_FailedToFindAdsObject = "Couldn't find the AdsObject with the distinguished name '{0}'.";
    private String m_FailedToAdd = "Couldn't add the object with the distinguished name '{0}' to the group.";
    private String m_FailedToRemove = "Couldn't remove the object with the distinguished name '{0}' from the group.";
    private String m_FailedToFindObject = "Couldn't find the object with the sAMAccountName '{0}'.";
    private String m_NoObjectsFound = "Couldn't find any objects.";
}

// INotifyPropertyChanged
partial class Localization : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(String propertyName) =>
        this.PropertyChanged?
            .Invoke(sender: this,
                    e: new(propertyName));
}