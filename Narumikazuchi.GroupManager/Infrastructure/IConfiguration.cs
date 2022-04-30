namespace Narumikazuchi.GroupManager;

public interface IConfiguration
{
    public void CopyFrom(IConfiguration configuration);

    public void Save();

    public String DefaultLocale
    {
        get;
        set;
    }

    public Boolean UseGroupsForPrincipals
    {
        get;
        set;
    }

    public String PrincipalsDn
    {
        get;
        set;
    }

    public Boolean UseGroupsForGroups
    {
        get;
        set;
    }

    public String ManagedGroupsDn
    {
        get;
        set;
    }
}