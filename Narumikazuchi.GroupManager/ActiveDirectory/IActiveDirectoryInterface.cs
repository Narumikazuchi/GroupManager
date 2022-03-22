namespace Narumikazuchi.GroupManager;

public interface IActiveDirectoryInterface
{
    public Boolean TryGetUserBySAMAccountName([DisallowNull] DirectoryEntry ou,
                                              [DisallowNull] String samAccountName,
                                              [NotNullWhen(true)] out DirectoryEntry? user);

    public Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry ou,
                                             [DisallowNull] String samAccountName,
                                             [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? groups);
    public Boolean TryGetGroupsManagedByUser([DisallowNull] DirectoryEntry ou,
                                             [DisallowNull] DirectoryEntry user,
                                             [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? groups);

    public Boolean TryGetObjectsFilteredBy([DisallowNull] DirectoryEntry ou,
                                           [DisallowNull] String filter,
                                           [NotNullWhen(true)] out IEnumerable<DirectoryEntry>? directoryObjects);

    public Boolean TryAddObjectToGroup([DisallowNull] DirectoryEntry group,
                                       [DisallowNull] String objectDn);

    public Boolean TryRemoveObjectFromGroup([DisallowNull] DirectoryEntry group,
                                            [DisallowNull] String objectDn);

    public IReadOnlyList<String> CastPropertyToStringArray([DisallowNull] DirectoryEntry directoryObject,
                                                           [DisallowNull] String property);
}