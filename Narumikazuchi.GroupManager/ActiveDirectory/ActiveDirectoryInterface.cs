namespace Narumikazuchi.GroupManager;

public static partial class ActiveDirectoryInterface
{
    public static IReadOnlyList<String> CastPropertyToStringArray([DisallowNull] DirectoryEntry adsObject!!,
                                                                  [DisallowNull] String property!!)
    {
        List<String> result = new();
        if (adsObject.Properties[property] is null ||
            adsObject.Properties[property].Value is null)
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

    public static Boolean TryAddPrincipalToGroup([DisallowNull] DirectoryEntry group!!,
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

    public static Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry user!!,
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

    public static Boolean TryGetPrincipalsFilteredBy([DisallowNull] DirectoryEntry groupOrOu!!,
                                                     [DisallowNull] String filter!!,
                                                     [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? principals)
    {
        if (String.IsNullOrWhiteSpace(filter))
        {
            principals = null;
            TextLogger.Instance
                      .Log("Couldn't get objects because the filter was empty and would've resulted in all objects in the OU.");
            return false;
        }

        String[] items = filter.Split(separator: new Char[] { ' ', ',' },
                                      options: StringSplitOptions.RemoveEmptyEntries);

        if (Configuration.Current.UseGroupsForPrincipals)
        {
            return TryGetPrincipalsFilteredByGroup(group: groupOrOu,
                                                   filter: items.Select(x => x.ToLower()),
                                                   principals: out principals);
        }
        else
        {
            return TryGetPrincipalsFilteredByOu(ou: groupOrOu,
                                                filter: items.Select(x => x.ToLower()),
                                                principals: out principals);
        }
    }

    public static Boolean TryGetPrincipalByDN([DisallowNull] String dn,
                                              [NotNullWhen(true)] out DirectoryEntry? principal)
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
                principal = null;
                TextLogger.Instance
                          .Log("The ou with the specified DN couldn't be found.");
                return false;
            }
            principal = result.GetDirectoryEntry();
            return true;
        }
        catch (Exception ex)
        {
            TextLogger.Instance
                      .Log(ex);
            principal = null;
            return false;
        }
    }

    public static Boolean TryRemovePrincipalFromGroup([DisallowNull] DirectoryEntry group,
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

    public static Boolean IsUser(this DirectoryEntry entry)
    {
        if (entry.Properties["objectClass"] is null ||
            entry.Properties["objectClass"].Value is null)
        {
            return false;
        }

        IReadOnlyList<String> classes = CastPropertyToStringArray(adsObject: entry,
                                                                  property: "objectClass");
        return classes.Any(x => x == "user");
    }

    public static Boolean IsGroup(this DirectoryEntry entry)
    {
        if (entry.Properties["objectClass"] is null ||
            entry.Properties["objectClass"].Value is null)
        {
            return false;
        }

        IReadOnlyList<String> classes = CastPropertyToStringArray(adsObject: entry,
                                                                  property: "objectClass");
        return classes.Any(x => x == "group");
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

    private static Boolean TryGetPrincipalsFilteredByOu(DirectoryEntry ou,
                                                        IEnumerable<String> filter,
                                                        out IEnumerable<DirectoryEntry> principals)
    {
        HashSet<DirectoryEntry> results = new(comparer: DirectoryEntryComparer.Default);

        try
        {
            using DirectorySearcher searcher = new(ou);

            foreach (String pattern in filter)
            {
                List<DirectoryEntry> tempResults = new();
                searcher.Filter = $"(&(&(|(objectClass=person)(objectClass=user)(objectClass=group))(!(objectClass=computer)))(|(sn={pattern}*)(givenName={pattern}*)(sAMAccountName={pattern}*)))";
                SearchResultCollection resultCollection = searcher.FindAll();
                foreach (SearchResult result in resultCollection)
                {
                    tempResults.Add(result.GetDirectoryEntry());
                }

                foreach (DirectoryEntry result in tempResults)
                {
                    results.Add(result);
                }
            }
        }
        catch (Exception ex)
        {
            TextLogger.Instance
                      .Log(ex);
            principals = Array.Empty<DirectoryEntry>();
            return false;
        }
        principals = results;
        return true;
    }

    private static Boolean TryGetPrincipalsFilteredByGroup(DirectoryEntry group,
                                                           IEnumerable<String> filter,
                                                           out IEnumerable<DirectoryEntry> principals)
    {
        List<DirectoryEntry> results = new();

        IReadOnlyList<String> memberDns = CastPropertyToStringArray(adsObject: group,
                                                                    property: "member");

        for (Int32 i = 0;
             i < memberDns.Count;
             i++)
        {
            try
            {
                if (!DirectoryEntry.Exists($"LDAP://{memberDns[i]}"))
                {
                    continue;
                }

                DirectoryEntry entry = new($"LDAP://{memberDns[i]}");

                Boolean added = false;
                foreach (String pattern in filter)
                {
                    if (entry.Properties["sn"].Value is not null
                                                     and String sn &&
                        sn.ToLower()
                          .StartsWith(pattern))
                    {
                        results.Add(entry);
                        added = true;
                        break;
                    }
                    if (entry.Properties["givenName"].Value is not null
                                                            and String givenName &&
                        givenName.ToLower()
                                 .StartsWith(pattern))
                    {
                        results.Add(entry);
                        added = true;
                        break;
                    }
                    if (entry.Properties["sAMAccountName"].Value is not null
                                                                 and String sAMAccountName &&
                        sAMAccountName.ToLower()
                                      .StartsWith(pattern))
                    {
                        results.Add(entry);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    entry.Dispose();
                }
            }
            catch (Exception ex)
            {
                TextLogger.Instance
                          .Log(ex);
            }
        }

        principals = results;
        return true;
    }
}