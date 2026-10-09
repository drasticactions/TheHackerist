using AvaEnyo;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using TheHackerist.Abstractions;
using TheHackerist.ViewModels;
using TheHackerist.Views;

namespace TheHackerist.UI.Tests;

public class ReaderTests : IDisposable
{
    private readonly Func<Control?> _factory = ReaderView.WebViewFactory;
    private readonly Func<Control, Task<bool>> _probe = ReaderView.StartProbe;

    public void Dispose()
    {
        ReaderView.WebViewFactory = _factory;
        ReaderView.StartProbe = _probe;
    }

    private static void UseFakeWebView(bool starts)
    {
        ReaderView.WebViewFactory = () => new Border { Name = "FakeWebView" };
        ReaderView.StartProbe = _ => Task.FromResult(starts);
    }

    [AvaloniaFact]
    public async Task LinkButton_StacksTheReader_AndBackReturnsToTheComments()
    {
        UseFakeWebView(starts: true);
        var f = new ShellFixture();
        await f.Shell.GoToStoryAsync(f.Client.Items[1]);
        f.Settle();

        TestHost.Click(f.Window, TestHost.Find<Button>(f.View, "LinkButton"));

        Assert.True(f.Pump(() => f.Shell.CurrentPage is ReaderViewModel));
        Assert.True(f.Pump(() => TestHost.All<ReaderView>(f.View).Any()));
        Assert.Equal([("https://example.com/one", BrowserMode.InApp)], f.Browser.Opened);

        EnyoBack.RequestBack(f.Window);
        f.Settle();

        Assert.IsType<StoryDetailViewModel>(f.Shell.CurrentPage);
        Assert.Single(f.Shell.Pages);
    }

    [AvaloniaFact]
    public async Task WebViewThatNeverStarts_ClosesTheReader_OpensExternally_AndHidesTheToggle()
    {
        UseFakeWebView(starts: false);
        var f = new ShellFixture();
        await f.Shell.GoToStoryAsync(f.Client.Items[1]);
        f.Settle();

        f.Shell.OpenReader(new Uri("https://example.com/one"));

        Assert.True(f.Pump(() => f.Shell.CurrentPage is StoryDetailViewModel));
        Assert.Contains(("https://example.com/one", BrowserMode.External), f.Browser.Opened);
        Assert.False(f.Features.SupportsInAppBrowser);
        Assert.False(f.Shell.Settings.CanUseInAppBrowser);
    }

    [AvaloniaFact]
    public async Task HeaderSpinner_FollowsTheLoadingState()
    {
        UseFakeWebView(starts: true);
        var f = new ShellFixture();
        await f.Shell.GoToStoryAsync(f.Client.Items[1]);
        f.Shell.OpenReader(new Uri("https://example.com/one"));
        Assert.True(f.Pump(() => TestHost.All<AvaEnyo.Controls.Spinner>(f.View).Any(s => s.Name == "PageLoadingSpinner")));

        var reader = f.Shell.CurrentReader!;
        var spinner = TestHost.Find<AvaEnyo.Controls.Spinner>(f.View, "PageLoadingSpinner");

        reader.IsLoading = true;
        f.Settle();
        Assert.True(spinner.IsEffectivelyVisible);

        reader.IsLoading = false;
        f.Settle();
        Assert.False(spinner.IsEffectivelyVisible);
    }
}
