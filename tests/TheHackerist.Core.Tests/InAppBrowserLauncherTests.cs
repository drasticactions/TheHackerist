using TheHackerist.Abstractions;
using TheHackerist.Services;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.ViewModels;

namespace TheHackerist.Tests;

public class InAppBrowserLauncherTests
{
    private static ShellViewModel CreateShell() =>
        new(new StubHackerNewsClient(), new StubBrowserLauncher(), new StubAppSettings(), new MutableTimeProvider());

    [Fact]
    public async Task InAppMode_StacksAReaderPageOnTheShell()
    {
        var shell = CreateShell();
        var launcher = new InAppBrowserLauncher(() => shell);

        await launcher.OpenAsync("https://example.com/article", BrowserMode.InApp);

        var reader = Assert.IsType<ReaderViewModel>(shell.CurrentPage);
        Assert.Equal("example.com", reader.Host);
        Assert.True(shell.CanGoBack);
    }

    [Fact]
    public async Task ExternalMode_LeavesTheShellUntouched()
    {
        var shell = CreateShell();
        var launcher = new InAppBrowserLauncher(() => shell);

        await launcher.OpenAsync("https://example.com/article", BrowserMode.External);

        Assert.Null(shell.CurrentPage);
    }

    [Fact]
    public async Task RelativeUrl_IsIgnoredRatherThanStacked()
    {
        var shell = CreateShell();
        var launcher = new InAppBrowserLauncher(() => shell);

        await launcher.OpenAsync("item?id=1", BrowserMode.InApp);

        Assert.Null(shell.CurrentPage);
    }
}
