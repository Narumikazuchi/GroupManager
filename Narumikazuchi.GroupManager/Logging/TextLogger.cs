using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

public sealed class TextLogger
{
    private TextLogger(DirectoryInfo directory!!)
    {
        this.Location = directory;
    }

    public void Log([DisallowNull] String message!!)
    {
        String path = Path.Combine(this.Location
                                       .FullName,
                                   DateOnly.FromDateTime(DateTime.Today)
                                           .ToString("yyyy-MM-dd") + ".log");
        using StreamWriter writer = File.AppendText(path);
        writer.Write(DateTime.Now.ToString());
        writer.Write(" === ");
        writer.WriteLine(message);
        writer.Flush();
    }
    public void Log<TException>([DisallowNull] TException exception!!)
        where TException : Exception
    {
        String path = Path.Combine(this.Location
                                       .FullName,
                                   DateOnly.FromDateTime(DateTime.Today)
                                           .ToString("yyyy-MM-dd") + ".log");
        using StreamWriter writer = File.AppendText(path);
        writer.Write(DateTime.Now.ToString());
        writer.Write(" === ");
        writer.WriteLine(exception.ToString());
        writer.Flush();
    }

    public static void Configure([DisallowNull] DirectoryInfo directory!!)
    {
        if (!directory.Exists)
        {
            Directory.CreateDirectory(directory.FullName);
        }
        s_Instance = new(directory);
    }

    public DirectoryInfo Location { get; }

    public static TextLogger Instance =>
        s_Instance;

    private static TextLogger s_Instance = new(new(Environment.CurrentDirectory));
}