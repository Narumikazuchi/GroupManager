namespace Narumikazuchi.GroupManager;

public abstract partial class AViewModel
{ }

// Non-Public
partial class AViewModel
{
    protected void OnPropertyChanged(String propertyName!!)
    {
        ExceptionHelpers.ThrowIfArgumentNull(propertyName);
        this.PropertyChanged?
            .Invoke(sender: this,
                    e: new(propertyName));
    }
}

// INotifyPropertyChanged
partial class AViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}