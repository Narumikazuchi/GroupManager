namespace Narumikazuchi.GroupManager;

public sealed class GroupListItemViewModel : AListItemViewModel
{
    public GroupListItemViewModel([DisallowNull] DirectoryEntry adsObject) :
        base(adsObject)
    { }

    public override String IconPath { get; } = "pack://application:,,,/Narumikazuchi.GroupManager;component/Resources/Group1.png";
}