namespace Narumikazuchi.GroupManager;

// Generic RelayCommand
public partial class RelayCommand<T>
{
    public RelayCommand(Action<T> onExecute!!) :
        this(onExecute: onExecute,
             canExecute: null)
    { }
    public RelayCommand(Action<T> onExecute!!,
                        Func<T, Boolean>? canExecute)
    {
        m_OnExecute = onExecute;
        m_CanExecute = canExecute;
    }
}

// Non-Public
partial class RelayCommand<T>
{
    private readonly Action<T> m_OnExecute;
    private readonly Func<T, Boolean>? m_CanExecute;
}

// ICommand
partial class RelayCommand<T> : ICommand
{
    public Boolean CanExecute(Object? parameter)
    {
        if (m_CanExecute is null)
        {
            return true;
        }
        if (parameter is not T myParameter)
        {
            return false;
        }
        if (m_CanExecute.Invoke(myParameter))
        {
            return true;
        }
        return false;
    }

    public void Execute(Object? parameter)
    {
        if (parameter is not T myParameter)
        {
            return;
        }
        m_OnExecute.Invoke(myParameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}