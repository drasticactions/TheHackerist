using TheHackerist.Abstractions;
using TheHackerist.ViewModels;

namespace TheHackerist.Services;

public sealed class InAppBrowserLauncher(Func<ShellViewModel> shell) : SystemBrowserLauncher
{
    public override Task OpenAsync(string url, BrowserMode mode)
    {
        if (mode != BrowserMode.InApp || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return base.OpenAsync(url, mode);
        }

        shell().OpenReader(uri);
        return Task.CompletedTask;
    }
}
