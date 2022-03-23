namespace Narumikazuchi.GroupManager;

public abstract partial class AListItemViewModel
{
    public AListItemViewModel([DisallowNull] DirectoryEntry adsObject)
    {
        this.Name = adsObject.Name
                             .Remove(0, 3);
        this.DisplayName = adsObject.Properties["displayName"]
                                    .Value?
                                    .ToString();
        this.AdsObject = adsObject;
    }

    public String Name { get; }

    public String? DisplayName { get; }

    public DirectoryEntry AdsObject { get; }

    public abstract String IconPath { get; }
}

// IDisposable
partial class AListItemViewModel : IDisposable
{
    public void Dispose()
    {
        this.AdsObject
            .Dispose();
        GC.SuppressFinalize(this);
    }
}