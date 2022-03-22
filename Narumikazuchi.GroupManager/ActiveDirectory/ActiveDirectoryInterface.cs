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
    public IReadOnlyList<String> CastPropertyToStringArray([DisallowNull] DirectoryEntry directoryObject!!,
                                                           [DisallowNull] String property!!)
    {
        List<String> result = new();
        if (directoryObject.Properties[property] is null)
        {
            return Array.Empty<String>();
        }

        if (directoryObject.Properties[property].Value is Object[] array)
        {
            result.AddRange(CastObjectArrayToStringArray(array));
        }
        else if (directoryObject.Properties[property].Value is String value)
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
        catch
        {
            // Write to log
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
            // Write to log
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
        List<String> groupNames = new(this.CastPropertyToStringArray(directoryObject: user,
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
        catch
        {
            groups = null;
            // Write to log
            return false;
        }

        groups = results;
        return true;
    }

    public Boolean TryGetObjectsFilteredBy([DisallowNull] DirectoryEntry ou!!,
                                           [DisallowNull] String filter!!,
                                           [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? directoryObjects)
    {
        if (String.IsNullOrWhiteSpace(filter))
        {
            directoryObjects = null;
            // Write to log
            return false;
        }

        String[] items = filter.Split(separator: new Char[] { ' ', ',' },
                                      options: StringSplitOptions.RemoveEmptyEntries);

        directoryObjects = new List<DirectoryEntry>();

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
                directoryObjects = directoryObjects.Intersect(second: tempResults,
                                                              comparer: DirectoryEntryComparer.Default);
            }
        }
        catch
        {
            // Write to log
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
                // Write to log
                return false;
            }
            user = result.GetDirectoryEntry();
            return true;
        }
        catch
        {
            user = null;
            // Write to log
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
        catch
        {
            // Write to log
            return false;
        }
    }
}