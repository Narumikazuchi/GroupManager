using Path = System.IO.Path;

namespace Narumikazuchi.GroupManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!Configuration.TryLoad())
        {
            ConfigurationWindow.Window window = new();
            window.ShowDialog();
            if (!window.DialogResult
                       .HasValue ||
                !window.DialogResult
                       .Value)
            {
                MessageBox.Show("Error");
                this.Shutdown();
                return;
            }
        }
        if (!Preferences.TryLoad())
        {
            Preferences.Current
                       .Save();
        }
        String path = Path.Combine(Environment.CurrentDirectory,
                                   $"{Preferences.Current.Locale}.locale");
        FileInfo file = new(path);
        if (!file.Exists)
        {
            MessageBox.Show("Error, file not found");
        }
        else if (!Localization.TryLoad(file))
        {
            MessageBox.Show("Error, displaying english");
        }

        this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        this.MainWindow = new MainWindow.Window();
        this.MainWindow
            .Show();
    }
}