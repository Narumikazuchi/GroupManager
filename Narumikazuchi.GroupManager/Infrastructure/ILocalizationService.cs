namespace Narumikazuchi.GroupManager;

public interface ILocalizationService
{
    public void LoadAvailableLanguageFiles();

    public void ChangeLocale(String? locale);

    public event Action? LocaleChanged;

    public event Action? LocaleListChanged;

    public IReadOnlyCollection<String> AvailableLocales { get; }

    public String SelectedLocale
    {
        get;
        set;
    }

    public IReadOnlyDictionary<String, String> LocalizationDictionary { get; }
}