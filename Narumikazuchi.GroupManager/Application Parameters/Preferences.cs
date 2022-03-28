using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

public sealed partial class Preferences
{
    static Preferences()
    {
        String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "Narumikazuchi",
                                   "GroupManager");
        s_Directory = new DirectoryInfo(path);
        path = Path.Combine(path,
                            "user.preference");
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

        writer.WriteStartElement("preferences");
        writer.WriteAttributeString(localName: "locale",
                                    value: this.Locale);
        writer.WriteStartElement("window");
        writer.WriteAttributeString(localName: "type",
                                    value: "main");
        writer.WriteAttributeString(localName: "x",
                                    value: this.MainWindowSize
                                               .X
                                               .ToString());
        writer.WriteAttributeString(localName: "y",
                                    value: this.MainWindowSize
                                               .Y
                                               .ToString());
        writer.WriteAttributeString(localName: "w",
                                    value: this.MainWindowSize
                                               .Width
                                               .ToString());
        writer.WriteAttributeString(localName: "h",
                                    value: this.MainWindowSize
                                               .Height
                                               .ToString());
        writer.WriteEndElement();
        writer.WriteStartElement("window");
        writer.WriteAttributeString(localName: "type",
                                    value: "add");
        writer.WriteAttributeString(localName: "x",
                                    value: this.AddUserWindowSize
                                               .X
                                               .ToString());
        writer.WriteAttributeString(localName: "y",
                                    value: this.AddUserWindowSize
                                               .Y
                                               .ToString());
        writer.WriteAttributeString(localName: "w",
                                    value: this.AddUserWindowSize
                                               .Width
                                               .ToString());
        writer.WriteAttributeString(localName: "h",
                                    value: this.AddUserWindowSize
                                               .Height
                                               .ToString());
        writer.WriteEndElement();
        writer.WriteStartElement("window");
        writer.WriteAttributeString(localName: "type",
                                    value: "group");
        writer.WriteAttributeString(localName: "x",
                                    value: this.GroupOverviewWindowSize
                                               .X
                                               .ToString());
        writer.WriteAttributeString(localName: "y",
                                    value: this.GroupOverviewWindowSize
                                               .Y
                                               .ToString());
        writer.WriteAttributeString(localName: "w",
                                    value: this.GroupOverviewWindowSize
                                               .Width
                                               .ToString());
        writer.WriteAttributeString(localName: "h",
                                    value: this.GroupOverviewWindowSize
                                               .Height
                                               .ToString());
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

        Rect main = new();
        Rect add = new();
        Rect group = new();
        String? locale = "en";
        while (reader.Read())
        {
            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "preferences")
            {
                locale = reader.GetAttribute("locale");
                if (locale is null)
                {
                    locale = "en";
                }
                continue;
            }
            String? type;
            String? sx;
            String? sy;
            String? sw;
            String? sh;
            if (reader.NodeType is XmlNodeType.Element &&
                reader.Name == "window")
            {
                type = reader.GetAttribute("type");
                if (type is null)
                {
                    return false;
                }
                sx = reader.GetAttribute("x");
                if (!Double.TryParse(s: sx,
                                     result: out Double x))
                {
                    return false;
                }
                sy = reader.GetAttribute("y");
                if (!Double.TryParse(s: sy,
                                     result: out Double y))
                {
                    return false;
                }
                sw = reader.GetAttribute("w");
                if (!Double.TryParse(s: sw,
                                     result: out Double w))
                {
                    return false;
                }
                sh = reader.GetAttribute("h");
                if (!Double.TryParse(s: sh,
                                     result: out Double h))
                {
                    return false;
                }
                switch (type)
                {
                    case "main":
                        main = new()
                        {
                            X = x,
                            Y = y,
                            Width = w,
                            Height = h
                        };
                        break;
                    case "add":
                        add = new()
                        {
                            X = x,
                            Y = y,
                            Width = w,
                            Height = h
                        };
                        break;
                    case "group":
                        group = new()
                        {
                            X = x,
                            Y = y,
                            Width = w,
                            Height = h
                        };
                        break;
                }
                continue;
            }
        }
        Current = new()
        {
            AddUserWindowSize = add,
            MainWindowSize = main,
            GroupOverviewWindowSize = group,
            Locale = locale
        };
        return true;
    }

    public static Preferences Current
    {
        get;
        set;
    } = new();

    public Rect AddUserWindowSize
    {
        get;
        set;
    }

    public Rect GroupOverviewWindowSize
    {
        get;
        set;
    }

    public Rect MainWindowSize
    {
        get;
        set;
    }

    public String Locale
    {
        get => m_Locale;
        set
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                m_Locale = "en";
                return;
            }
            m_Locale = value;
        }
    }
}

// Non-Public
partial class Preferences
{
    private static readonly DirectoryInfo s_Directory;
    private static readonly FileInfo s_File;
    private String m_Locale = "en";
}