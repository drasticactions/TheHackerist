using TheHackerist.Abstractions;
using TheHackerist.Models;
using TheHackerist.Services;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.ViewModels;

namespace TheHackerist.Tests;

public class FeedViewModelTests
{
    private sealed class RecordingBrowser : IBrowserLauncher
    {
        public List<(string Url, BrowserMode Mode)> Opened { get; } = [];

        public Task OpenAsync(string url, BrowserMode mode)
        {
            Opened.Add((url, mode));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingNavigator : INavigator
    {
        public List<HnItem> Stories { get; } = [];

        public Task GoToStoryAsync(HnItem story)
        {
            Stories.Add(story);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedSettings : IAppSettings
    {
        public BrowserMode BrowserMode { get; set; } = BrowserMode.InApp;

        public ThemePreference ThemePreference { get; set; }
    }

    private static HnItem Story(long id, string? url = "https://example.com") =>
        new() { Id = id, Type = "story", Title = $"Story {id}", By = "user", Time = 1753600000, Url = url };

    private static (FeedViewModel Vm, StubHackerNewsClient Client, RecordingBrowser Browser, RecordingNavigator Nav, FixedSettings Settings) Create(int storyCount)
    {
        var client = new StubHackerNewsClient();
        var ids = Enumerable.Range(1, storyCount).Select(i => (long)i).ToArray();
        client.Feeds[FeedType.Top] = ids;
        foreach (var id in ids)
        {
            client.AddItem(Story(id));
        }

        var browser = new RecordingBrowser();
        var nav = new RecordingNavigator();
        var settings = new FixedSettings();
        var vm = new FeedViewModel(FeedType.Top, client, browser, settings, nav, new MutableTimeProvider());
        return (vm, client, browser, nav, settings);
    }

    [Fact]
    public async Task Refresh_LoadsFirstPageAndSetsHasMore()
    {
        var (vm, _, _, _, _) = Create(storyCount: 45);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(30, vm.Stories.Count);
        Assert.True(vm.HasMore);
        Assert.Equal("Story 1", vm.Stories[0].Title);
        Assert.Equal(1, vm.Stories[0].Rank);
        Assert.False(vm.IsRefreshing);
    }

    [Fact]
    public async Task LoadMore_AppendsRemainingStories()
    {
        var (vm, _, _, _, _) = Create(storyCount: 45);
        await vm.RefreshCommand.ExecuteAsync(null);

        await vm.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(45, vm.Stories.Count);
        Assert.False(vm.HasMore);
        Assert.Equal(31, vm.Stories[30].Rank);
    }

    [Fact]
    public async Task EnsureLoaded_OnlyLoadsOnce()
    {
        var (vm, _, _, _, _) = Create(storyCount: 5);

        await vm.EnsureLoadedAsync();
        await vm.EnsureLoadedAsync();

        Assert.Equal(5, vm.Stories.Count);
    }

    [Fact]
    public async Task OpenStory_WithUrl_NavigatesToComments()
    {
        var (vm, _, browser, nav, _) = Create(storyCount: 1);
        await vm.RefreshCommand.ExecuteAsync(null);

        await vm.OpenStoryCommand.ExecuteAsync(vm.Stories[0]);

        Assert.Empty(browser.Opened);
        Assert.Equal([1L], nav.Stories.Select(s => s.Id));
    }

    [Fact]
    public async Task OpenArticle_WithUrl_UsesBrowserWithConfiguredMode()
    {
        var (vm, _, browser, nav, settings) = Create(storyCount: 1);
        settings.BrowserMode = BrowserMode.External;
        await vm.RefreshCommand.ExecuteAsync(null);

        await vm.OpenArticleCommand.ExecuteAsync(vm.Stories[0]);

        Assert.Equal([("https://example.com", BrowserMode.External)], browser.Opened);
        Assert.Empty(nav.Stories);
    }

    [Fact]
    public async Task OpenArticle_WithoutUrl_NavigatesToComments()
    {
        var (vm, client, browser, nav, _) = Create(storyCount: 0);
        client.Feeds[FeedType.Top] = [10];
        client.AddItem(Story(10, url: null));
        await vm.RefreshCommand.ExecuteAsync(null);

        await vm.OpenArticleCommand.ExecuteAsync(vm.Stories[0]);

        Assert.Empty(browser.Opened);
        Assert.Equal([10L], nav.Stories.Select(s => s.Id));
    }

    [Fact]
    public async Task SelectingAStory_OpensItsComments()
    {
        var (vm, _, _, nav, _) = Create(storyCount: 2);
        await vm.RefreshCommand.ExecuteAsync(null);

        vm.SelectedStory = vm.Stories[1];

        Assert.Equal([2L], nav.Stories.Select(s => s.Id));
    }

    [Fact]
    public async Task Refresh_ClearsTheSelection()
    {
        var (vm, _, _, _, _) = Create(storyCount: 2);
        await vm.RefreshCommand.ExecuteAsync(null);
        vm.SelectedStory = vm.Stories[0];

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Null(vm.SelectedStory);
    }

    [Fact]
    public async Task NetworkFailure_SetsErrorAndNotifiesOncePerFeed()
    {
        var client = new StubHackerNewsClient { Failure = new HttpRequestException("offline") };
        var notifier = new StubUserNotifier();
        var vm = new FeedViewModel(FeedType.New, client, new RecordingBrowser(), new FixedSettings(), new RecordingNavigator(), new MutableTimeProvider(), notifier);

        await vm.RefreshCommand.ExecuteAsync(null);
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.True(vm.IsEmpty);
        Assert.Single(notifier.Shown);
        Assert.Equal("feed:New", notifier.Shown.Keys.Single());
    }

    [Fact]
    public async Task Retry_AfterRecovery_ClearsErrorAndNotification()
    {
        var (_, client, browser, nav, settings) = Create(storyCount: 3);
        var notifier = new StubUserNotifier();
        var vm = new FeedViewModel(FeedType.Top, client, browser, settings, nav, new MutableTimeProvider(), notifier);
        client.Failure = new HttpRequestException("offline");
        await vm.RefreshCommand.ExecuteAsync(null);

        client.Failure = null;
        await notifier.Shown["feed:Top"].Retry!();

        Assert.Null(vm.ErrorMessage);
        Assert.Equal(3, vm.Stories.Count);
        Assert.Empty(notifier.Shown);
    }
}
