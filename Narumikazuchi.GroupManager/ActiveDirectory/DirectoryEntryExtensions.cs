namespace Narumikazuchi.GroupManager;

public static class DirectoryEntryExtensions
{
    public static Boolean IsUser(this DirectoryEntry entry,
                                 IActiveDirectoryService service)
    {
        if (entry.Properties["objectClass"] is null ||
            entry.Properties["objectClass"].Value is null)
        {
            return false;
        }

        IReadOnlyList<String> classes = service.CastPropertyToStringArray(adsObject: entry,
                                                                          property: "objectClass");
        return classes.Any(x => x == "user");
    }

    public static Boolean IsGroup(this DirectoryEntry entry,
                                  IActiveDirectoryService service)
    {
        if (entry.Properties["objectClass"] is null ||
            entry.Properties["objectClass"].Value is null)
        {
            return false;
        }

        IReadOnlyList<String> classes = service.CastPropertyToStringArray(adsObject: entry,
                                                                          property: "objectClass");
        return classes.Any(x => x == "group");
    }
}