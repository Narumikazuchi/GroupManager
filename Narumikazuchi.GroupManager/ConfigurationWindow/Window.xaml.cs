using WpfWindow = System.Windows.Window;

namespace Narumikazuchi.GroupManager.ConfigurationWindow;

/// <summary>
/// Interaction logic for Window.xaml
/// </summary>
public partial class Window : WpfWindow
{
    public Window(ViewModel model)
    {
        this.DataContext = model;
        this.InitializeComponent();
        BorderlessWindowResizer.AttachTo(this);
        ThemeWatcher.Instance
                    .WindowsThemeChanged += (s, e) => this.Dispatcher.Invoke(this.UpdateLayout);
    }
}