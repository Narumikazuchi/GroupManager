namespace Narumikazuchi.GroupManager;

public static partial class ActiveDirectoryInterface
{
    public static IReadOnlyList<String> CastPropertyToStringArray([DisallowNull] DirectoryEntry adsObject!!,
                                                                  [DisallowNull] String property!!)
    {
        List<String> result = new();
        if (adsObject.Properties[property] is null)
        {
            return Array.Empty<String>();
        }

        if (adsObject.Properties[property].Value is Object[] array)
        {
            result.AddRange(CastObjectArrayToStringArray(array));
        }
        else if (adsObject.Properties[property].Value is String value)
        {
            result.Add(value);
        }

        return result;
    }

    public static Boolean TryAddObjectToGroup([DisallowNull] DirectoryEntry group!!,
                                              [DisallowNull] String objectDn!!)
    {
        group.Properties["member"]
             .Add(objectDn);
        try
        {
            group.CommitChanges();
            return true;
        }
        catch (Exception ex)
        {
            TextLogger.Instance
                      .Log(ex);
            return false;
        }
    }

    public static Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry ou!!,
                                                    [DisallowNull] String samAccountName!!,
                                                    [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? groups)
    {
        if (!TryGetUserBySAMAccountName(ou: ou,
                                        samAccountName: samAccountName,
                                        user: out DirectoryEntry? user))
        {
            groups = null;
            TextLogger.Instance
                      .Log("Couldn't get groups because the user with the specified sAMAccountName couldn't be found.");
            return false;
        }

        return TryGetGroupsManagedByUser(ou: ou,
                                         user: user,
                                         groups: out groups);
    }

    public static Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry ou!!,
                                                    [DisallowNull] DirectoryEntry user!!,
                                                    [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? groups)
    {
        IReadOnlyList<String> groupDns = CastPropertyToStringArray(adsObject: user,
                                                                   property: "managedObjects");
        List<DirectoryEntry> results = new();
        foreach (String groupDn in groupDns)
        {
            DirectoryEntry group = new($"LDAP://{groupDn}");
            Object? value = group.Properties["objectClass"]
                                 .Value;
            if (value is Object[] &&
                CastPropertyToStringArray(adsObject: group,
                                          property: "objectClass").Contains("group"))
            {
                results.Add(group);
                continue;
            }
            if (value?.ToString() == "group")
            {
                results.Add(group);
            }
        }

        groups = results;
        return true;
    }

    public static Boolean TryGetObjectsFilteredBy([DisallowNull] DirectoryEntry ou!!,
                                                  [DisallowNull] String filter!!,
                                                  in Boolean findGroups,
                                                  [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? adsObjects)
    {
        if (String.IsNullOrWhiteSpace(filter))
        {
            adsObjects = null;
            TextLogger.Instance
                      .Log("Couldn't get objects because the filter was empty and would've resulted in all objects in the OU.");
            return false;
        }

        String[] items = filter.Split(separator: new Char[] { ' ', ',' },
                                      options: StringSplitOptions.RemoveEmptyEntries);

        adsObjects = new List<DirectoryEntry>();

        try
        {
            using DirectorySearcher searcher = new(ou);

            for (Int32 i = 0;
                 i < items.Length;
                 i++)
            {
                List<DirectoryEntry> tempResults = new();
                searcher.Filter = ComposeFilterString(pattern: items[i],
                                                      group: findGroups);
                SearchResultCollection resultCollection = searcher.FindAll();
                foreach (SearchResult result in resultCollection)
                {
                    tempResults.Add(result.GetDirectoryEntry());
                }
                adsObjects = adsObjects.Union(second: tempResults,
                                              comparer: DirectoryEntryComparer.Default);
            }
        }
        catch (Exception ex)
        {
            TextLogger.Instance
                      .Log(ex);
            return false;
        }

        return true;
    }

    public static Boolean TryGetUserBySAMAccountName([DisallowNull] DirectoryEntry ou!!,
                                                     [DisallowNull] String samAccountName,
                                                     [NotNullWhen(true)] out DirectoryEntry? user)
    {
        try
        {
            using DirectorySearcher searcher = new(ou)
            {
                Filter = $"(&(|(objectClass=person)(objectClass=user))(sAMAccountName={samAccountName}))"
            };
            SearchResult? result = searcher.FindOne();
            if (result is null)
            {
                user = null;
                TextLogger.Instance
                          .Log("The user with the specified sAMAccountName couldn't be found.");
                return false;
            }
            user = result.GetDirectoryEntry();
            return true;
        }
        catch (Exception ex)
        {
            TextLogger.Instance
                      .Log(ex);
            user = null;
            return false;
        }
    }

    public static Boolean TryGetOU([DisallowNull] String dn,
                                   [NotNullWhen(true)] out DirectoryEntry? ou)
    {
        try
        {
            using DirectorySearcher searcher = new()
            {
                Filter = $"(distinguishedName={dn})"
            };
            SearchResult? result = searcher.FindOne();
            if (result is null)
            {
                ou = null;
                TextLogger.Instance
                          .Log("The ou with the specified DN couldn't be found.");
                return false;
            }
            ou = result.GetDirectoryEntry();
            return true;
        }
        catch (Exception ex)
        {
            TextLogger.Instance
                      .Log(ex);
            ou = null;
            return false;
        }
    }

    public static Boolean TryRemoveObjectFromGroup([DisallowNull] DirectoryEntry group,
                                                   [DisallowNull] String objectDn)
    {
        group.Properties["member"]
             .Remove(objectDn);
        try
        {
            group.CommitChanges();
            return true;
        }
        catch (Exception ex)
        {
            TextLogger.Instance
                      .Log(ex);
            return false;
        }
    }
}

// Non-Public
partial class ActiveDirectoryInterface
{
    private static IEnumerable<String> CastObjectArrayToStringArray(Object[] array!!)
    {
        List<String> result = new();
        for (Int32 i = 0;
             i < array.Length;
             i++)
        {
            if (array[i] is String item)
            {
                result.Add(item);
                continue;
            }
            if (array[i] is Object[] subarray)
            {
                result.AddRange(CastObjectArrayToStringArray(subarray));
                continue;
            }
        }
        return result;
    }

    private static String ComposeFilterString(String pattern,
                                              in Boolean group)
    {
        if (group)
        {
            return $"(&(&(objectClass=group)(!(objectClass=computer)))(|(sn={pattern}*)(givenName={pattern}*)(sAMAccountName={pattern}*)))";
        }
        return $"(&(&(|(objectClass=person)(objectClass=user))(!(objectClass=computer)))(|(sn={pattern}*)(givenName={pattern}*)(sAMAccountName={pattern}*)))";
    }
}