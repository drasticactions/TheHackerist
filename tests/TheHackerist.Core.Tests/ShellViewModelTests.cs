using TheHackerist.Abstractions;
using TheHackerist.Models;
using TheHackerist.Services;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.ViewModels;

namespace TheHackerist.Tests;

public class ShellViewModelTests
{
    private static (ShellViewModel Shell, StubHackerNewsClient Client) CreateShell()
    {
        var client = new StubHackerNewsClient();
        client.Feeds[FeedType.Top] = [1];
        client.Feeds[FeedType.New] = [2];
        client.AddItem(new HnItem { Id = 1, Type = "story", Title = "Top story" });
        client.AddItem(new HnItem { Id = 2, Type = "story", Title = "New story" });

        var shell = new ShellViewModel(client, new StubBrowserLauncher(), new StubAppSettings(), new MutableTimeProvider());
        return (shell, client);
    }

    private static int IndexOf(ShellViewModel shell, FeedType feed) =>
        shell.Feeds.Select((f, i) => (f, i)).Single(p => p.f.Feed == feed).i;

    [Fact]
    public void NewShell_StartsOnTopFeedWithNothingStacked()
    {
        var (shell, _) = CreateShell();

        Assert.Equal(FeedType.Top, shell.CurrentFeed.Feed);
        Assert.Equal(Enum.GetValues<FeedType>(), shell.Feeds.Select(f => f.Feed));
        Assert.True(shell.IsHome);
        Assert.Null(shell.CurrentPage);
        Assert.False(shell.CanGoBack);
    }

    [Fact]
    public async Task ShowFeeds_LoadsOnlyTheFeedsOnScreen()
    {
        var (shell, client) = CreateShell();

        await shell.ShowFeedsAsync(IndexOf(shell, FeedType.Top), IndexOf(shell, FeedType.New));

        Assert.Equal(2, client.GetFeedIdsCallCount);
        Assert.Single(shell.Feeds[IndexOf(shell, FeedType.Top)].Stories);
        Assert.Single(shell.Feeds[IndexOf(shell, FeedType.New)].Stories);
        Assert.Empty(shell.Feeds[IndexOf(shell, FeedType.Best)].Stories);
    }

    [Fact]
    public async Task SelectingAFeed_ReusesTheCachedViewModelAndDoesNotRefetch()
    {
        var (shell, client) = CreateShell();
        await shell.InitializeAsync();

        var originalTop = shell.CurrentFeed;
        var feedIdCallsAfterFirstLoad = client.GetFeedIdsCallCount;

        shell.SelectedFeedIndex = IndexOf(shell, FeedType.New);
        await shell.ShowFeedsAsync(shell.SelectedFeedIndex, shell.SelectedFeedIndex);
        var newFeed = shell.CurrentFeed;

        shell.SelectedFeedIndex = IndexOf(shell, FeedType.Top);
        await shell.ShowFeedsAsync(shell.SelectedFeedIndex, shell.SelectedFeedIndex);

        Assert.NotSame(originalTop, newFeed);
        Assert.Same(originalTop, shell.CurrentFeed);
        Assert.Equal(feedIdCallsAfterFirstLoad + 1, client.GetFeedIdsCallCount);
        Assert.Single(shell.CurrentFeed.Stories);
    }

    [Fact]
    public async Task Refresh_OnHomeRefetchesEveryVisibleFeedAndNoOther()
    {
        var (shell, client) = CreateShell();
        await shell.ShowFeedsAsync(IndexOf(shell, FeedType.Top), IndexOf(shell, FeedType.New));
        var callsBefore = client.GetFeedIdsCallCount;

        await shell.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(callsBefore + 2, client.GetFeedIdsCallCount);
        Assert.Empty(shell.Feeds[IndexOf(shell, FeedType.Best)].Stories);
    }

    [Fact]
    public async Task Refresh_OnAStoryReloadsItsCommentsInsteadOfTheFeeds()
    {
        var (shell, client) = CreateShell();
        await shell.GoToStoryAsync(new HnItem { Id = 1, Type = "story", Title = "Top story", Kids = [] });
        var feedCallsBefore = client.GetFeedIdsCallCount;
        var itemCallsBefore = client.GetItemsCallCount;

        await shell.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(feedCallsBefore, client.GetFeedIdsCallCount);
        Assert.True(client.GetItemsCallCount > itemCallsBefore);
    }

    [Fact]
    public async Task GoToStory_PushesADetailPage()
    {
        var (shell, _) = CreateShell();

        await shell.GoToStoryAsync(new HnItem { Id = 1, Type = "story", Title = "Top story" });

        var detail = Assert.IsType<StoryDetailViewModel>(shell.CurrentPage);
        Assert.Equal("Top story", detail.Title);
        Assert.Same(detail, shell.CurrentStory);
        Assert.False(shell.IsHome);
        Assert.True(shell.CanGoBack);
    }

    [Fact]
    public async Task Stories_NavigateThroughTheFeedViewModel()
    {
        var (shell, _) = CreateShell();
        await shell.InitializeAsync();

        await shell.CurrentFeed.OpenStoryCommand.ExecuteAsync(shell.CurrentFeed.Stories[0]);

        Assert.IsType<StoryDetailViewModel>(shell.CurrentPage);
    }

    [Fact]
    public async Task GoToStory_ReplacesTheOpenStory()
    {
        var (shell, _) = CreateShell();
        await shell.GoToStoryAsync(new HnItem { Id = 1, Type = "story", Title = "A" });
        shell.OpenReader(new Uri("https://example.com/a"));

        await shell.GoToStoryAsync(new HnItem { Id = 2, Type = "story", Title = "B" });

        var detail = Assert.IsType<StoryDetailViewModel>(Assert.Single(shell.Pages));
        Assert.Equal("B", detail.Title);
    }

    [Fact]
    public async Task GoToStory_ForTheOpenStory_ClosesTheReaderWithoutRefetching()
    {
        var (shell, client) = CreateShell();
        var story = new HnItem { Id = 1, Type = "story", Title = "A", Kids = [] };
        await shell.GoToStoryAsync(story);
        var detail = shell.CurrentStory;
        shell.OpenReader(new Uri("https://example.com/a"));
        var itemCalls = client.GetItemsCallCount;

        await shell.GoToStoryAsync(story);

        Assert.Same(detail, shell.CurrentPage);
        Assert.Single(shell.Pages);
        Assert.Equal(itemCalls, client.GetItemsCallCount);
    }

    [Fact]
    public async Task OpenReader_StacksOnTheStoryAndBackReturnsToIt()
    {
        var (shell, _) = CreateShell();
        await shell.GoToStoryAsync(new HnItem { Id = 1, Type = "story", Title = "A" });

        shell.OpenReader(new Uri("https://example.com/a"));
        Assert.IsType<ReaderViewModel>(shell.CurrentPage);

        shell.GoBackCommand.Execute(null);
        Assert.Equal("A", shell.CurrentStory?.Title);

        shell.GoBackCommand.Execute(null);
        Assert.Null(shell.CurrentPage);
        Assert.True(shell.IsHome);
        Assert.False(shell.CanGoBack);
    }

    [Fact]
    public async Task UnavailableWebView_ClosesTheReaderAndOpensExternally()
    {
        var browser = new StubBrowserLauncher();
        var features = new PlatformFeatures();
        var notifier = new StubUserNotifier();
        var shell = new ShellViewModel(new StubHackerNewsClient(), browser, new StubAppSettings(), new MutableTimeProvider(), notifier, features);
        await shell.GoToStoryAsync(new HnItem { Id = 1, Type = "story", Title = "A" });
        shell.OpenReader(new Uri("https://example.com/a"));

        await shell.CurrentReader!.ReportWebViewUnavailableAsync();

        Assert.IsType<StoryDetailViewModel>(shell.CurrentPage);
        Assert.False(features.SupportsInAppBrowser);
        Assert.False(shell.Settings.CanUseInAppBrowser);
        Assert.Equal([("https://example.com/a", BrowserMode.External)], browser.Opened);
        Assert.Single(notifier.Shown);
    }

    [Fact]
    public void GoBack_OnAnEmptyStackIsHarmless()
    {
        var (shell, _) = CreateShell();

        shell.GoBackCommand.Execute(null);

        Assert.Null(shell.CurrentPage);
        Assert.False(shell.CanGoBack);
    }

    [Fact]
    public void OpenReader_ExposesTheReaderForTheAppBar()
    {
        var (shell, _) = CreateShell();

        shell.OpenReader(new Uri("https://example.com/post"));

        Assert.Equal("example.com", shell.CurrentReader?.Host);
        Assert.Null(shell.CurrentStory);
    }

    [Fact]
    public void OpenSettings_OpensPreferencesWithoutStackingAPage()
    {
        var (shell, _) = CreateShell();

        shell.OpenSettingsCommand.Execute(null);

        Assert.True(shell.IsSettingsOpen);
        Assert.Empty(shell.Pages);
        Assert.True(shell.IsHome);
    }
}
