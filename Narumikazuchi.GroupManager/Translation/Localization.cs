namespace Narumikazuchi.GroupManager;

public class Localization
{
    public static Localization Instance { get; } = new();

    public String MainTitle { get; } = "Group Manager";

    public String ReloadList { get; } = "Reload list";

    public String ShowMembers { get; } = "Show members";

    public String Close { get; } = "Close";

    public String Add { get; } = "Add";

    public String Remove { get; } = "Remove";

    public String Cancel { get; } = "Cancel";

    public String FilterLabel { get; } = "Filter by: ";

    public String ResultsLabel { get; } = "Results: ";
}