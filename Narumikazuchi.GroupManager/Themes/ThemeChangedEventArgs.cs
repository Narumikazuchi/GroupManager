namespace Narumikazuchi.GroupManager;

public sealed class ThemeChangedEventArgs : EventArgs
{
    public ThemeChangedEventArgs(WindowsTheme theme)
    {
        this.Theme = theme;
    }

    public WindowsTheme Theme { get; }
}