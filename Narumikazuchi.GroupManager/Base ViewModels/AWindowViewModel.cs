namespace Narumikazuchi.GroupManager;

public abstract partial class AWindowViewModel
{
    public ICommand OpenSystemMenuCommand =>
        m_OpenSystemMenuCommand;

    public ICommand MinimizeWindowCommand =>
        m_MinimizeWindowCommand;

    public ICommand MaximizeWindowCommand =>
        m_MaximizeWindowCommand;

    public ICommand CloseWindowCommand =>
        m_CloseWindowCommand;
}

// Non-Public
partial class AWindowViewModel : AViewModel
{
    protected AWindowViewModel()
    {
        m_OpenSystemMenuCommand = new(this.OpenSystemMenu);
        m_MinimizeWindowCommand = new(this.MinimizeWindow);
        m_MaximizeWindowCommand = new(this.MaximizeWindow);
        m_CloseWindowCommand = new(this.CloseWindow);
    }

    private static Point GetMouseLocation(Window window!!)
    {
        Point temp = Mouse.GetPosition(window);
        return new(x: temp.X + window.Left, 
                   y: temp.Y + window.Top);
    }

    protected virtual void OpenSystemMenu(Window window!!)
    {
        if (window is null)
        {
            return;
        }
        SystemCommands.ShowSystemMenu(window: window,
                                      screenLocation: GetMouseLocation(window));
    }

    protected virtual void MinimizeWindow(Window window!!)
    {
        if (window is null)
        {
            return;
        }
        SystemCommands.MinimizeWindow(window);
    }

    protected virtual void MaximizeWindow(Window window!!)
    {
        if (window is null)
        {
            return;
        }
        if (window.WindowState is WindowState.Normal)
        {
            SystemCommands.MaximizeWindow(window);
            return;
        }
        window.WindowState = WindowState.Normal;
    }

    protected virtual void CloseWindow(Window window!!)
    {
        if (window is null)
        {
            return;
        }
        SystemCommands.CloseWindow(window);
    }

    private readonly RelayCommand<Window> m_OpenSystemMenuCommand;
    private readonly RelayCommand<Window> m_MinimizeWindowCommand;
    private readonly RelayCommand<Window> m_MaximizeWindowCommand;
    private readonly RelayCommand<Window> m_CloseWindowCommand;
}