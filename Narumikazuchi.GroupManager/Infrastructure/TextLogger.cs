using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

public sealed partial class TextLogger
{
    public TextLogger()
    {
        this.Location = new(Environment.CurrentDirectory);
    }
}

// ILogger
partial class TextLogger : ILogger
{
    public void Log([DisallowNull] String message)
    {
        ArgumentNullException.ThrowIfNull(message);

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
    public void Log<TException>([DisallowNull] TException exception)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(exception);

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

    public DirectoryInfo Location
    {
        get;
        set;
    }
}