using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Styling;
using TheHackerist.Services;
using TheHackerist.ViewModels;
using TheHackerist.Views;

namespace TheHackerist;

public partial class App : Application
{
    private ServiceProvider? _services;

    public Action<IServiceCollection>? ConfigurePlatformServices { get; set; }

    public ThemeVariant? ThemeOverride { get; set; }

    public Uri? StartupUrl { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = AppServices.Build(ConfigurePlatformServices);
        _services.GetRequiredService<AppSettings>().ApplyTheme();
        if (ThemeOverride is { } variant)
        {
            RequestedThemeVariant = variant;
        }

        var shell = _services.GetRequiredService<ShellViewModel>();
        var notifier = _services.GetRequiredService<DelegatingUserNotifier>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(notifier) { DataContext = shell };
            desktop.ShutdownRequested += (_, _) => _services?.Dispose();
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activityLifetime)
        {
            activityLifetime.MainViewFactory = () => new MainView { DataContext = shell, Notifier = notifier };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView { DataContext = shell, Notifier = notifier };
        }

        base.OnFrameworkInitializationCompleted();

        _ = shell.InitializeAsync();

        if (StartupUrl is { } url)
        {
            shell.OpenReader(url);
        }
    }
}
