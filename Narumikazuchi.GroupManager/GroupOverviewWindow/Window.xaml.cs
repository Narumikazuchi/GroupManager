﻿using WpfWindow = System.Windows.Window;

namespace Narumikazuchi.GroupManager.GroupOverviewWindow;

/// <summary>
/// Interaction logic for Window.xaml
/// </summary>
public partial class Window : WpfWindow
{
    public Window()
    {
        this.InitializeComponent();
        ThemeWatcher.Instance
                    .WindowsThemeChanged += (s, e) => this.Dispatcher.Invoke(this.UpdateLayout);
    }
}