using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using AvaEnyo;
using Avalonia;
using Avalonia.Browser;
using Microsoft.Extensions.DependencyInjection;
using TheHackerist;
using TheHackerist.Abstractions;
using TheHackerist.Browser;
using TheHackerist.Services;

[assembly: SupportedOSPlatform("browser")]

internal static class Program
{
    private static async Task Main(string[] args)
    {
        await JSHost.ImportAsync(LocalStorageSettingsStore.ModuleName, "../settings.js");

        await BuildAvaloniaApp()
            .WithAvaEnyoFonts()
            .AfterSetup(builder => ((App)builder.Instance!).ConfigurePlatformServices = ConfigureServices)
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IBrowserLauncher, SystemBrowserLauncher>();
        services.AddSingleton(new PlatformFeatures(supportsInAppBrowser: false));
        services.AddSingleton<ISettingsStore, LocalStorageSettingsStore>();
    }
}
