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
        writer.WriteStartElement("userou");
        writer.WriteAttributeString(localName: "dn",
                                    value: m_UserOuDn);
        writer.WriteEndElement();
        writer.WriteStartElement("groupou");
        writer.WriteAttributeString(localName: "dn",
                                    value: m_GroupOuDn);
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

        String userOu = String.Empty;
        String groupOu = String.Empty;
        while (reader.Read())
        {
            String? dn;
            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "userou")
            {
                dn = reader.GetAttribute("dn");
                if (dn is null)
                {
                    return false;
                }
                userOu = dn;
                continue;
            }
            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "groupou")
            {
                dn = reader.GetAttribute("dn");
                if (dn is null)
                {
                    return false;
                }
                groupOu = dn;
                continue;
            }
        }
        Current = new()
        {
            UserOuDn = userOu,
            GroupOuDn = groupOu
        };
        return true;
    }

    public static Configuration Current
    {
        get;
        set;
    } = new();

    public String UserOuDn
    {
        get => $"LDAP://{m_UserOuDn}";
        set => m_UserOuDn = value;
    }

    public String GroupOuDn
    {
        get => $"LDAP://{m_GroupOuDn}";
        set => m_GroupOuDn = value;
    }
}

// Non-Public
partial class Configuration
{
    private static readonly DirectoryInfo s_Directory;
    private static readonly FileInfo s_File;
    private String m_UserOuDn = String.Empty;
    private String m_GroupOuDn = String.Empty;
}