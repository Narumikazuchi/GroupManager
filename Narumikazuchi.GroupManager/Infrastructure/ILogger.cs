namespace Narumikazuchi.GroupManager;

public interface ILogger
{
    public void Log(String message);
    public void Log<TException>(TException exception)
        where TException : Exception;

    public DirectoryInfo Location
    {
        get;
        set;
    }
}