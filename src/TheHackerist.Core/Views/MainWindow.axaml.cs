using AvaEnyo;
using Avalonia.Controls;
using TheHackerist.Services;

namespace TheHackerist.Views;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(null)
    {
    }

    public MainWindow(DelegatingUserNotifier? notifier)
    {
        InitializeComponent();
        Main.Notifier = notifier;
        EnyoWindow.SetAppName(this, "Gallery");
        EnyoWindow.SetUseEnyoDecorations(this, true);
    }

    public MainView View => Main;
}
