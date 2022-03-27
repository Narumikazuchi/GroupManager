namespace Narumikazuchi.GroupManager;

public sealed class Configuration
{
    public void Save()
    {

    }

    public static Configuration Current
    {
        get;
        set;
    } = new();

    public String UserOuDn
    {
        get;
        set;
    }

    public String GroupOuDn
    {
        get;
        set;
    }
}