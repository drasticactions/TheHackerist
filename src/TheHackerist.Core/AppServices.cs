using Microsoft.Extensions.DependencyInjection;
using TheHackerist.Abstractions;
using TheHackerist.Services;
using TheHackerist.ViewModels;

namespace TheHackerist;

public static class AppServices
{
    public static ServiceProvider Build(Action<IServiceCollection>? configurePlatformServices = null)
    {
        var services = new ServiceCollection();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = HackerNewsClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(30),
        });
        services.AddSingleton<IHackerNewsClient, HackerNewsClient>();
        services.AddSingleton(new PlatformFeatures());
        services.AddSingleton<ISettingsStore>(_ => new FileSettingsStore(FileSettingsStore.DefaultPath()));
        services.AddSingleton<DelegatingUserNotifier>();
        services.AddSingleton<IUserNotifier>(sp => sp.GetRequiredService<DelegatingUserNotifier>());
        services.AddSingleton<AppSettings>();
        services.AddSingleton<IAppSettings>(sp => sp.GetRequiredService<AppSettings>());
        services.AddSingleton<IBrowserLauncher>(sp => new InAppBrowserLauncher(sp.GetRequiredService<ShellViewModel>));

        configurePlatformServices?.Invoke(services);

        services.AddSingleton<ShellViewModel>();

        return services.BuildServiceProvider();
    }
}
