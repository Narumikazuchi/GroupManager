namespace Narumikazuchi.GroupManager;

public sealed partial class ActiveDirectoryService : IActiveDirectoryService
{
    public ActiveDirectoryService(ILogger logger,
                                  IConfiguration configuration)
    {
        m_Logger = logger;
        m_Configuration = configuration;
    }

    public IReadOnlyList<String> CastPropertyToStringArray([DisallowNull] DirectoryEntry adsObject,
                                                           [DisallowNull] String property)
    {
        ArgumentNullException.ThrowIfNull(adsObject);
        ArgumentNullException.ThrowIfNull(property);

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

    public Boolean TryAddPrincipalToGroup([DisallowNull] DirectoryEntry group,
                                          [DisallowNull] String objectDn)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(objectDn);

        group.Properties["member"]
             .Add(objectDn);
        try
        {
            group.CommitChanges();
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.Log(ex);
            return false;
        }
    }

    public Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry user,
                                             [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? groups)
    {
        ArgumentNullException.ThrowIfNull(user);

        IReadOnlyList<String> groupDns = this.CastPropertyToStringArray(adsObject: user,
                                                                        property: "managedObjects");
        List<DirectoryEntry> results = new();
        foreach (String groupDn in groupDns)
        {
            DirectoryEntry group = new($"LDAP://{groupDn}");
            Object? value = group.Properties["objectClass"]
                                 .Value;
            if (value is Object[] &&
                this.CastPropertyToStringArray(adsObject: group,
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

    public Boolean TryGetPrincipalsFilteredBy([DisallowNull] DirectoryEntry groupOrOu,
                                              [DisallowNull] String filter,
                                              [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? principals)
    {
        ArgumentNullException.ThrowIfNull(groupOrOu);
        ArgumentNullException.ThrowIfNull(filter);

        if (String.IsNullOrWhiteSpace(filter))
        {
            principals = null;
            m_Logger.Log("Couldn't get objects because the filter was empty and would've resulted in all objects in the OU.");
            return false;
        }

        String[] items = filter.Split(separator: new Char[] { ' ', ',' },
                                      options: StringSplitOptions.RemoveEmptyEntries);

        if (m_Configuration.UseGroupsForPrincipals)
        {
            return this.TryGetPrincipalsFilteredByGroup(group: groupOrOu,
                                                        filter: items.Select(x => x.ToLower()),
                                                        principals: out principals);
        }
        else
        {
            return this.TryGetPrincipalsFilteredByOu(ou: groupOrOu,
                                                     filter: items.Select(x => x.ToLower()),
                                                     principals: out principals);
        }
    }

    public Boolean TryGetPrincipalByDN([DisallowNull] String dn,
                                       [NotNullWhen(true)] out DirectoryEntry? principal)
    {
        ArgumentNullException.ThrowIfNull(dn);

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
                m_Logger.Log("The ou with the specified DN couldn't be found.");
                return false;
            }
            principal = result.GetDirectoryEntry();
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.Log(ex);
            principal = null;
            return false;
        }
    }

    public Boolean TryRemovePrincipalFromGroup([DisallowNull] DirectoryEntry group,
                                               [DisallowNull] String objectDn)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(objectDn);

        group.Properties["member"]
             .Remove(objectDn);
        try
        {
            group.CommitChanges();
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.Log(ex);
            return false;
        }
    }
}

// Non-Public
partial class ActiveDirectoryService
{
    private static IEnumerable<String> CastObjectArrayToStringArray(Object[] array)
    {
        ArgumentNullException.ThrowIfNull(array);

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

    private Boolean TryGetPrincipalsFilteredByOu(DirectoryEntry ou,
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
            m_Logger.Log(ex);
            principals = Array.Empty<DirectoryEntry>();
            return false;
        }
        principals = results;
        return true;
    }

    private Boolean TryGetPrincipalsFilteredByGroup(DirectoryEntry group,
                                                    IEnumerable<String> filter,
                                                    out IEnumerable<DirectoryEntry> principals)
    {
        List<DirectoryEntry> results = new();

        IReadOnlyList<String> memberDns = this.CastPropertyToStringArray(adsObject: group,
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
                m_Logger.Log(ex);
            }
        }

        principals = results;
        return true;
    }

    private readonly ILogger m_Logger;
    private readonly IConfiguration m_Configuration;
}