namespace Narumikazuchi.GroupManager;

// Simple RelayCommand
public partial class RelayCommand
{
    public RelayCommand(Action onExecute!!) :
        this(onExecute: onExecute,
             canExecute: null)
    { }
    public RelayCommand(Action onExecute!!,
                        Func<Boolean>? canExecute)
    {
        m_OnExecute = onExecute;
        m_CanExecute = canExecute;
    }
}

// Non-Public
partial class RelayCommand
{
    private readonly Action m_OnExecute;
    private readonly Func<Boolean>? m_CanExecute;
}

// ICommand
partial class RelayCommand : ICommand
{
    public Boolean CanExecute(Object? parameter)
    {
        if (m_CanExecute is null ||
            m_CanExecute.Invoke())
        {
            return true;
        }
        return false;
    }

    public void Execute(Object? parameter) => 
        m_OnExecute.Invoke();

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}