namespace Narumikazuchi.GroupManager;
public sealed class DirectoryEntryComparer : IEqualityComparer<DirectoryEntry>
{
    private DirectoryEntryComparer()
    { }

    public Boolean Equals(DirectoryEntry? x,
                          DirectoryEntry? y)
    {
        if (x is null)
        {
            return y is null;
        }
        if (y is null)
        {
            return false;
        }
        return x.Guid.Equals(y.Guid);
    }

    public Int32 GetHashCode([DisallowNull] DirectoryEntry obj) =>
        obj.Guid
           .GetHashCode();

    public static DirectoryEntryComparer Default { get; } = new DirectoryEntryComparer();
}