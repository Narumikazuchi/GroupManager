namespace Narumikazuchi.GroupManager;

public sealed class Preferences
{
    public void Save()
    {

    }

    public static Preferences Current
    {
        get;
        set;
    } = new();

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
}