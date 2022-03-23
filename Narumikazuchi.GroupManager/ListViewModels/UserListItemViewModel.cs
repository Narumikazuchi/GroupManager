namespace Narumikazuchi.GroupManager;

public sealed class UserListItemViewModel : AListItemViewModel
{
    public UserListItemViewModel([DisallowNull] DirectoryEntry adsObject) :
        base(adsObject)
    { }

    public override String IconPath { get; } = "pack://application:,,,/Narumikazuchi.GroupManager;component/Resources/User1.png";
}