using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

public sealed partial class Configuration
{
    static Configuration()
    {
        String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                   "Narumikazuchi",
                                   "GroupManager");
        s_Directory = new DirectoryInfo(path);
        path = Path.Combine(path,
                            "global.config");
        s_File = new(path);
    }

    public void Save()
    {
        if (!s_Directory.Exists)
        {
            Directory.CreateDirectory(s_Directory.FullName);
        }

        using FileStream stream = new(path: s_File.FullName,
                                      mode: FileMode.Create,
                                      access: FileAccess.Write,
                                      share: FileShare.Write);
        using XmlTextWriter writer = new(w: stream,
                                         encoding: Encoding.UTF8)
        {
            Formatting = Formatting.Indented,
            Indentation = 4
        };

        writer.WriteStartElement("configuration");
        writer.WriteAttributeString(localName: "locale",
                                    value: this.DefaultLocale);

        writer.WriteStartElement("userdn");
        writer.WriteAttributeString(localName: "dn",
                                    value: this.PrincipalsDn);
        if (this.UseGroupsForPrincipals)
        {
            writer.WriteAttributeString(localName: "useGroup",
                                        value: "true");
        }
        writer.WriteEndElement();
        writer.WriteStartElement("groupdn");
        writer.WriteAttributeString(localName: "dn",
                                    value: this.ManagedGroupsDn);
        if (this.UseGroupsForGroups)
        {
            writer.WriteAttributeString(localName: "useGroup",
                                        value: "true");
        }
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.Flush();
    }

    public static Boolean TryLoad()
    {
        if (!s_File.Exists)
        {
            return false;
        }

        using FileStream stream = new(path: s_File.FullName,
                                      mode: FileMode.Open,
                                      access: FileAccess.Read,
                                      share: FileShare.Read);
        using XmlReader reader = XmlReader.Create(input: stream);

        String locale = String.Empty;
        String userDn = String.Empty;
        String groupDn = String.Empty;
        Boolean useGroupForUsers = false;
        Boolean useGroupForGroups = false;
        while (reader.Read())
        {
            String? dn;
            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "configuration")
            {
                String? defaultLocale = reader.GetAttribute("locale");
                if (defaultLocale is null)
                {
                    locale = "en";
                    continue;
                }
                locale = defaultLocale;
                continue;
            }

            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "userdn")
            {
                String? useGroupAttr = reader.GetAttribute("useGroup");
                if (useGroupAttr is not null)
                {
                    useGroupForUsers = true;
                }

                dn = reader.GetAttribute("dn");
                if (dn is null)
                {
                    return false;
                }
                userDn = dn;
                continue;
            }
            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "groupdn")
            {
                String? useGroupAttr = reader.GetAttribute("useGroup");
                if (useGroupAttr is not null)
                {
                    useGroupForGroups = true;
                }

                dn = reader.GetAttribute("dn");
                if (dn is null)
                {
                    return false;
                }
                groupDn = dn;
                continue;
            }
        }

        if (!Localization.AvailableLanguages.Any(x => x == locale))
        {
            locale = "en";
        }

        Current = new()
        {
            DefaultLocale = locale,
            UseGroupsForPrincipals = useGroupForUsers,
            PrincipalsDn = userDn,
            UseGroupsForGroups = useGroupForGroups,
            ManagedGroupsDn = groupDn
        };

        return true;
    }

    public static Configuration Current
    {
        get;
        set;
    } = new();

    public String DefaultLocale
    {
        get;
        set;
    } = "en";

    public Boolean UseGroupsForPrincipals
    {
        get;
        set;
    }

    public String PrincipalsDn
    {
        get;
        set;
    } = String.Empty;

    public Boolean UseGroupsForGroups
    {
        get;
        set;
    }

    public String ManagedGroupsDn
    {
        get;
        set;
    } = String.Empty;
}

// Non-Public
partial class Configuration
{
    private static readonly DirectoryInfo s_Directory;
    private static readonly FileInfo s_File;
}