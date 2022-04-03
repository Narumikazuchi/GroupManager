namespace Narumikazuchi.GroupManager;

public sealed partial class Localization
{
    public static Boolean TryLoad(FileInfo file)
    {
        if (!file.Exists)
        {
            return false;
        }
        
        using FileStream stream = new(path: file.FullName,
                                      mode: FileMode.Open,
                                      access: FileAccess.Read,
                                      share: FileShare.Read);
        using XmlReader reader = XmlReader.Create(input: stream);

        Localization localization = new();
        while (reader.Read())
        {
            String? key;
            String? value;
            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "text")
            {
                key = reader.GetAttribute("key");
                if (key is null)
                {
                    return false;
                }
                value = reader.GetAttribute("value");
                if (value is null)
                {
                    return false;
                }
                switch (key)
                {
                    case nameof(MainTitle):
                        localization.m_MainTitle = value;
                        break;
                    case nameof(ReloadList):
                        localization.m_ReloadList = value;
                        break;
                    case nameof(ShowMembers):
                        localization.m_ShowMembers = value;
                        break;
                    case nameof(Close):
                        localization.m_Close = value;
                        break;
                    case nameof(Add):
                        localization.m_Add = value;
                        break;
                    case nameof(Remove):
                        localization.m_Remove = value;
                        break;
                    case nameof(Cancel):
                        localization.m_Cancel = value;
                        break;
                    case nameof(FilterLabel):
                        localization.m_FilterLabel = value;
                        break;
                    case nameof(ResultsLabel):
                        localization.m_ResultsLabel = value;
                        break;
                    case nameof(Configuration):
                        localization.m_Configuration = value;
                        break;
                    case nameof(UserOuDn):
                        localization.m_UserOuDn = value;
                        break;
                    case nameof(GroupOuDn):
                        localization.m_GroupOuDn = value;
                        break;
                    case nameof(Save):
                        localization.m_Save = value;
                        break;
                    case nameof(Manager):
                        localization.m_Manager = value;
                        break;
                    case nameof(Unknown):
                        localization.m_Unknown = value;
                        break;
                    case nameof(MembersOf):
                        localization.m_MembersOf = value;
                        break;
                    case nameof(Group):
                        localization.m_Group = value;
                        break;
                    case nameof(Status):
                        localization.m_Status = value;
                        break;
                    case nameof(StatusLoading):
                        localization.m_StatusLoading = value;
                        break;
                    case nameof(StatusDone):
                        localization.m_StatusDone = value;
                        break;
                    case nameof(FailedToOpenOU):
                        localization.m_FailedToOpenOU = value;
                        break;
                    case nameof(FailedToAdd):
                        localization.m_FailedToAdd = value;
                        break;
                    case nameof(FailedToRemove):
                        localization.m_FailedToRemove = value;
                        break;
                    case nameof(FailedToFindObject):
                        localization.m_FailedToFindObject = value;
                        break;
                    case nameof(NoObjectsFound):
                        localization.m_NoObjectsFound = value;
                        break;
                }
                continue;
            }
        }
        Instance = localization;
        return true;
    }

    public static Localization Instance
    {
        get;
        set;
    } = new();

    public String MainTitle =>
        m_MainTitle;

    public String ReloadList =>
        m_ReloadList;

    public String ShowMembers =>
        m_ShowMembers;

    public String Close =>
        m_Close;

    public String Add =>
        m_Add;

    public String Remove =>
        m_Remove;

    public String Cancel =>
        m_Cancel;

    public String FilterLabel =>
        m_FilterLabel;

    public String ResultsLabel =>
        m_ResultsLabel;

    public String Configuration =>
        m_Configuration;

    public String UserOuDn =>
        m_UserOuDn;

    public String GroupOuDn =>
        m_GroupOuDn;

    public String Save =>
        m_Save;

    public String Manager =>
        m_Manager;

    public String Unknown =>
        m_Unknown;

    public String MembersOf =>
        m_MembersOf;

    public String Group =>
        m_Group;

    public String Status =>
        m_Status;

    public String StatusLoading =>
        m_StatusLoading;

    public String StatusDone =>
        m_StatusDone;

    public String FailedToOpenOU =>
        m_FailedToOpenOU;

    public String FailedToAdd =>
        m_FailedToAdd;

    public String FailedToRemove =>
        m_FailedToRemove;

    public String FailedToFindObject =>
        m_FailedToFindObject;

    public String NoObjectsFound =>
        m_NoObjectsFound;
}

// Non-Public
partial class Localization
{
    private String m_MainTitle = "Group Manager";
    private String m_ReloadList = "Reload list";
    private String m_ShowMembers = "Show members";
    private String m_Close = "Close";
    private String m_Add = "Add";
    private String m_Remove = "Remove";
    private String m_Cancel = "Cancel";
    private String m_FilterLabel = "Filter by: ";
    private String m_ResultsLabel = "Results: ";
    private String m_Configuration = "Configuration";
    private String m_UserOuDn = "Distinguished Name of the Organisational Unit containing all users:";
    private String m_GroupOuDn = "Distinguished Name of the Organisational Unit containing all groups:";
    private String m_Save = "Save";
    private String m_Manager = "Manager: {0}";
    private String m_Unknown = "unknown";
    private String m_MembersOf = "Members of {0}";
    private String m_Group = "Group: {0}";
    private String m_Status = "Status: {0}";
    private String m_StatusLoading = "loading...";
    private String m_StatusDone = "done";
    private String m_FailedToOpenOU = "Couldn't open the OU with the distinguished name '{0}'.";
    private String m_FailedToAdd = "Couldn't add the object with the distinguished name '{0}' to the group.";
    private String m_FailedToRemove = "Couldn't remove the object with the distinguished name '{0}' from the group.";
    private String m_FailedToFindObject = "Couldn't the object with the sAMAccountName '{0}'.";
    private String m_NoObjectsFound = "Couldn't find anz objects.";
}