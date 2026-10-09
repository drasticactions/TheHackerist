using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using TheHackerist.Abstractions;

namespace TheHackerist.Services;

public class SystemBrowserLauncher : IBrowserLauncher
{
    public virtual async Task OpenAsync(string url, BrowserMode mode)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        if (TopLevel.GetTopLevel(GetVisualRoot()) is { Launcher: { } launcher })
        {
            await launcher.LaunchUriAsync(uri);
        }
    }

    protected static Control? GetVisualRoot() =>
        Avalonia.Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
            ISingleViewApplicationLifetime singleView => singleView.MainView,
            _ => null,
        };
}
