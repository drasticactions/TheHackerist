using TheHackerist.Abstractions;

namespace TheHackerist.Tests.TestDoubles;

public sealed class StubBrowserLauncher : IBrowserLauncher
{
    public List<(string Url, BrowserMode Mode)> Opened { get; } = [];

    public Task OpenAsync(string url, BrowserMode mode)
    {
        Opened.Add((url, mode));
        return Task.CompletedTask;
    }
}

public sealed class StubAppSettings : IAppSettings
{
    public BrowserMode BrowserMode { get; set; }

    public ThemePreference ThemePreference { get; set; }
}
