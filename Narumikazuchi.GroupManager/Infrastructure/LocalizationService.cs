using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

public sealed partial class LocalizationService
{
    public LocalizationService(IConfiguration configuration,
                               IPreferences preferences)
    {
        m_Configuration = configuration;
        m_Preferences = preferences;

        m_Languages = new(LoadLanguages);
        this.LoadAvailableLanguageFiles();
    }
}

// Non-Public
partial class LocalizationService
{
    private static Boolean TryLoad(FileInfo file,
                                   [NotNullWhen(true)] out Dictionary<String, String>? dictionary)
    {
        if (!file.Exists)
        {
            dictionary = null;
            return false;
        }

        dictionary = new();
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
                    continue;
                }
                value = reader.GetAttribute("value");
                if (value is null)
                {
                    continue;
                }
                dictionary[key] = value;
            }
        }

        return true;
    }

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

    private static Boolean IsValid(Dictionary<String, String> dictionary)
    {
        String[] keys = {
            "MainTitle",
            "ReloadList",
            "ShowMembers",
            "Close",
            "Add",
            "Remove",
            "Cancel",
            "FilterLabel",
            "ResultsLabel",
            "Configuration",
            "DefaultLocale",
            "UseGroup",
            "PrincipalOuDn",
            "GroupOuDn",
            "PrincipalGroupDn",
            "GroupsGroupDn",
            "Save",
            "Manager",
            "Unknown",
            "MembersOf",
            "Group",
            "Status",
            "StatusLoading",
            "StatusDone",
            "UserIsNotPartOfDomain",
            "FailedToFindAdsObject",
            "FailedToAdd",
            "FailedToRemove",
            "FailedToFindObject",
            "NoObjectsFound"
        };

        foreach (String key in keys)
        {
            if (!dictionary.ContainsKey(key))
            {
                return false;
            }
        }

        return true;
    }

    private void GenerateDefaultLocale()
    {
        Dictionary<String, String> map = new();
        m_Locales.Add("en",
                      map);

        map["MainTitle"] = "Group Manager";
        map["ReloadList"] = "Reload list";
        map["ShowMembers"] = "Show members";
        map["Close"] = "Close";
        map["Add"] = "Add";
        map["Remove"] = "Remove";
        map["Cancel"] = "Cancel";
        map["FilterLabel"] = "Filter by:";
        map["ResultsLabel"] = "Results:";
        map["Configuration"] = "Configuration";
        map["DefaultLocale"] = "Default language:";
        map["UseGroup"] = "Use Security Group";
        map["PrincipalOuDn"] = "Distinguished Name of the Organisational Unit containing the accessible principals:";
        map["GroupOuDn"] = "Distinguished Name of the Organisational Unit containing the managed groups:";
        map["PrincipalGroupDn"] = "Distinguished Name of the Security Group that contains the accessible principals:";
        map["GroupsGroupDn"] = "Distinguished Name of the Security Group that contains the managed groups:";
        map["Save"] = "Save";
        map["Manager"] = "Manager: {0}";
        map["Unknown"] = "unknown";
        map["MembersOf"] = "Members of {0}";
        map["Group"] = "Group: {0}";
        map["Status"] = "Status: {0}";
        map["StatusLoading"] = "loading...";
        map["StatusDone"] = "done";
        map["UserIsNotPartOfDomain"] = "The currently logged in user is not part of a domain.";
        map["FailedToFindAdsObject"] = "Couldn't find the AdsObject with the distinguished name '{0}'.";
        map["FailedToAdd"] = "Couldn't add the object with the distinguished name '{0}' to the group.";
        map["FailedToRemove"] = "Couldn't remove the object with the distinguished name '{0}' from the group.";
        map["FailedToFindObject"] = "Couldn't find the object with the sAMAccountName '{0}'.";
        map["NoObjectsFound"] = "Couldn't find any objects.";
    }

    private readonly Dictionary<String, Dictionary<String, String>> m_Locales = new();
    private readonly IConfiguration m_Configuration;
    private readonly IPreferences m_Preferences;
    private readonly Lazy<IReadOnlyList<String>> m_Languages;
    private String m_Locale = "en";
}

// INotifyPropertyChanged
partial class LocalizationService : ILocalizationService
{
    public void ChangeLocale(String? locale)
    {
        if (locale is null)
        {
            locale = m_Configuration.DefaultLocale;
            m_Preferences.Locale = null;
        }

        if (m_Locales.ContainsKey(locale))
        {
            m_Locale = locale;
            this.LocaleChanged?
                .Invoke();
            m_Preferences.Locale = locale;
            return;
        }
        else
        {
            m_Locale = "en";
            this.LocaleChanged?
                .Invoke();
            m_Preferences.Locale = locale;
        }
    }

    public void LoadAvailableLanguageFiles()
    {
        m_Locales.Clear();
        foreach (String locale in m_Languages.Value)
        {
            if (locale == "en")
            {
                this.GenerateDefaultLocale();
                continue;
            }

            FileInfo file = new(Path.Combine(Environment.CurrentDirectory,
                                             $"{locale}.locale"));

            if (!TryLoad(file: file,
                         dictionary: out Dictionary<String, String>? dictionary))
            {
                continue;
            }

            if (!IsValid(dictionary))
            {
                continue;
            }

            m_Locales.Add(key: locale,
                          value: dictionary);
        }
        this.LocaleListChanged?
            .Invoke();
    }

    public event Action? LocaleChanged;

    public event Action? LocaleListChanged;

    public IReadOnlyCollection<String> AvailableLocales =>
        m_Locales.Keys;

    public String SelectedLocale
    {
        get => m_Locale;
        set => this.ChangeLocale(value);
    }

    public IReadOnlyDictionary<String, String> LocalizationDictionary =>
        m_Locales[m_Locale];
}