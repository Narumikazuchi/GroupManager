namespace Narumikazuchi.GroupManager;

public sealed partial class ActiveDirectoryInterface
{
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

    private static String ComposeFilterString(String parameter)
    {
        StringBuilder builder = new();
        builder.Append("(&(&(|(objectClass=person)(objectClass=group))(!(objectClass=computer)))(|");
        builder.Append($"(sn={parameter}*)");
        builder.Append($"(givenName={parameter}*)");
        builder.Append($"(sAMAccountName={parameter}*)");
        builder.Append("))");
        return builder.ToString();
    }
}

// IActiveDirectoryInterface
partial class ActiveDirectoryInterface : IActiveDirectoryInterface
{
    public IReadOnlyList<String> CastPropertyToStringArray([DisallowNull] DirectoryEntry adsObject!!,
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

    public Boolean TryAddObjectToGroup([DisallowNull] DirectoryEntry group!!,
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

    public Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry ou!!,
                                             [DisallowNull] String samAccountName!!,
                                             [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? groups)
    {
        if (!this.TryGetUserBySAMAccountName(ou: ou,
                                             samAccountName: samAccountName, 
                                             user: out DirectoryEntry? user))
        {
            groups = null;
            TextLogger.Instance
                      .Log("Couldn't get groups because the user with the specified sAMAccountName couldn't be found.");
            return false;
        }

        return this.TryGetGroupsManagedByUser(ou: ou,
                                              user: user,
                                              groups: out groups);
    }

    public Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry ou!!,
                                             [DisallowNull] DirectoryEntry user!!,
                                             [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? groups)
    {

        // Remove the LDAP:// at the start of the path
        String groupManager = user.Path
                                  .ToString()
                                  .Remove(0, 7);
        List<String> groupNames = new(this.CastPropertyToStringArray(adsObject: user,
                                                                     property: "memberOf"))
        {
            groupManager
        };

        List<DirectoryEntry> results = new();
        try
        {
            using DirectorySearcher searcher = new(ou)
            {
                Filter = "(objectClass=group)"
            };
            SearchResultCollection resultCollection = searcher.FindAll();
            foreach (SearchResult result in resultCollection)
            {
                DirectoryEntry group = result.GetDirectoryEntry();
                String? managedBy = group.Properties["managedBy"]
                                         .Value?
                                         .ToString();
                if (managedBy is not null &&
                    groupNames.Contains(managedBy))
                {
                    results.Add(group);
                    continue;
                }
                group.Dispose();
            }
        }
        catch (Exception ex)
        {
            TextLogger.Instance
                      .Log(ex);
            groups = null;
            return false;
        }

        groups = results;
        return true;
    }

    public Boolean TryGetObjectsFilteredBy([DisallowNull] DirectoryEntry ou!!,
                                           [DisallowNull] String filter!!,
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

            for (Int32 i = 1;
                 i < items.Length;
                 i++)
            {
                List<DirectoryEntry> tempResults = new();
                searcher.Filter = ComposeFilterString(items[i]);
                SearchResultCollection resultCollection = searcher.FindAll();
                foreach (SearchResult result in resultCollection)
                {
                    tempResults.Add(result.GetDirectoryEntry());
                }
                adsObjects = adsObjects.Intersect(second: tempResults,
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

    public Boolean TryGetUserBySAMAccountName([DisallowNull] DirectoryEntry ou!!, 
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

    public Boolean TryRemoveObjectFromGroup([DisallowNull] DirectoryEntry group,
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