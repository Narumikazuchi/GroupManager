using WpfWindow = System.Windows.Window;

namespace Narumikazuchi.GroupManager.AddMemberWindow;

/// <summary>
/// Interaction logic for Window.xaml
/// </summary>
public partial class Window : WpfWindow
{
    public Window(ViewModel viewModel)
    {
        this.DataContext = viewModel;
        this.InitializeComponent();
        BorderlessWindowResizer.AttachTo(this);
        ThemeWatcher.Instance
                    .WindowsThemeChanged += (s, e) => this.Dispatcher.Invoke(this.UpdateLayout);
    }
}