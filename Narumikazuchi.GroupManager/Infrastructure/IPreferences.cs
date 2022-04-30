namespace Narumikazuchi.GroupManager;

public interface IPreferences
{
    public void Save();

    public Rect AddUserWindowSize
    {
        get;
        set;
    }

    public Rect GroupOverviewWindowSize
    {
        get;
        set;
    }

    public Rect MainWindowSize
    {
        get;
        set;
    }

    public String? Locale
    {
        get;
        set;
    }
}