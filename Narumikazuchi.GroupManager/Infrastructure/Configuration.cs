using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

public sealed partial class Configuration : IConfiguration
{
    static Configuration()
    {
        String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                   "Narumikazuchi",
                                   "GroupManager");
        s_Directory = new DirectoryInfo(path);
        path = Path.Combine(s_Directory.FullName,
                            "global.config");
        DefaultLocation = new(path);
    }

    public Configuration()
    {
        m_File = DefaultLocation;
    }
    public Configuration(FileInfo file)
    {
        m_File = file;
    }

    public static Boolean TryLoad(FileInfo file,
                                  [NotNullWhen(true)] out Configuration? configuration)
    {
        if (!file.Exists)
        {
            configuration = null;
            return false;
        }

        using FileStream stream = new(path: file.FullName,
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
                    configuration = null;
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
                    configuration = null;
                    return false;
                }
                groupDn = dn;
                continue;
            }
        }

        configuration = new()
        {
            DefaultLocale = locale,
            UseGroupsForPrincipals = useGroupForUsers,
            PrincipalsDn = userDn,
            UseGroupsForGroups = useGroupForGroups,
            ManagedGroupsDn = groupDn
        };

        return true;
    }

    public static FileInfo DefaultLocation { get; }
}

// Non-Public
partial class Configuration
{
    private static readonly DirectoryInfo s_Directory;
    private readonly FileInfo m_File;
}

// IConfiguration
partial class Configuration : IConfiguration
{
    public void CopyFrom(IConfiguration configuration)
    {
        this.DefaultLocale = configuration.DefaultLocale;
        this.UseGroupsForPrincipals = configuration.UseGroupsForPrincipals;
        this.PrincipalsDn = configuration.PrincipalsDn;
        this.UseGroupsForGroups = configuration.UseGroupsForGroups;
        this.ManagedGroupsDn = configuration.ManagedGroupsDn;
    }

    public void Save()
    {
        if (!s_Directory.Exists)
        {
            Directory.CreateDirectory(s_Directory.FullName);
        }

        using FileStream stream = new(path: m_File.FullName,
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