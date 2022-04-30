namespace Narumikazuchi.GroupManager;

public interface IActiveDirectoryService
{
    public IReadOnlyList<String> CastPropertyToStringArray([DisallowNull] DirectoryEntry adsObject,
                                                           [DisallowNull] String property);

    public Boolean TryAddPrincipalToGroup([DisallowNull] DirectoryEntry group,
                                          [DisallowNull] String distinguishedName);

    public Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry user,
                                             [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? groups);

    public Boolean TryGetPrincipalsFilteredBy([DisallowNull] DirectoryEntry groupOrOu,
                                              [DisallowNull] String filter,
                                              [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? principals);

    public Boolean TryGetPrincipalByDN([DisallowNull] String distinguishedName,
                                       [NotNullWhen(true)] out DirectoryEntry? principal);

    public Boolean TryRemovePrincipalFromGroup([DisallowNull] DirectoryEntry group,
                                               [DisallowNull] String distinguishedName);
}