using TheHackerist.Abstractions;
using TheHackerist.Models;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.ViewModels;

namespace TheHackerist.Tests;

public class StoryDetailViewModelTests
{
    private sealed class NoopBrowser : IBrowserLauncher
    {
        public Task OpenAsync(string url, BrowserMode mode) => Task.CompletedTask;
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
        public BrowserMode BrowserMode { get; set; }

        public ThemePreference ThemePreference { get; set; }
    }

    private static HnItem Comment(long id, string text, params long[] kids) => new()
    {
        Id = id,
        Type = "comment",
        By = $"user{id}",
        Time = 1753600000,
        Text = text,
        Kids = kids.Length > 0 ? kids : null,
    };

    [Fact]
    public async Task Initialize_FlattensCommentTreeDepthFirstWithDepths()
    {
        var client = new StubHackerNewsClient();
        client.AddItem(Comment(1, "first", 11));
        client.AddItem(Comment(11, "reply", 111));
        client.AddItem(Comment(111, "deep reply"));
        client.AddItem(Comment(2, "second"));

        var story = new HnItem { Id = 100, Type = "story", Title = "T", Kids = [1, 2], Descendants = 4 };
        var vm = new StoryDetailViewModel(client, new NoopBrowser(), new FixedSettings(), new MutableTimeProvider());

        await vm.InitializeAsync(story);

        Assert.Equal(["first", "reply", "deep reply", "second"], vm.Comments.Select(c => c.Text));
        Assert.Equal([0, 1, 2, 0], vm.Comments.Select(c => c.Depth));
        Assert.False(vm.HasMoreComments);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task Initialize_SkipsDeletedComments()
    {
        var client = new StubHackerNewsClient();
        client.AddItem(Comment(1, "kept"));
        client.AddItem(new HnItem { Id = 2, Type = "comment", Deleted = true });

        var story = new HnItem { Id = 100, Type = "story", Kids = [1, 2] };
        var vm = new StoryDetailViewModel(client, new NoopBrowser(), new FixedSettings(), new MutableTimeProvider());

        await vm.InitializeAsync(story);

        Assert.Equal(["kept"], vm.Comments.Select(c => c.Text));
    }

    [Fact]
    public async Task LoadMore_PagesTopLevelComments()
    {
        var client = new StubHackerNewsClient();
        var kids = Enumerable.Range(1, 20).Select(i => (long)i).ToArray();
        foreach (var id in kids)
        {
            client.AddItem(Comment(id, $"c{id}"));
        }

        var story = new HnItem { Id = 100, Type = "story", Kids = kids };
        var vm = new StoryDetailViewModel(client, new NoopBrowser(), new FixedSettings(), new MutableTimeProvider());

        await vm.InitializeAsync(story);
        Assert.Equal(15, vm.Comments.Count);
        Assert.True(vm.HasMoreComments);

        await vm.LoadMoreCommentsCommand.ExecuteAsync(null);
        Assert.Equal(20, vm.Comments.Count);
        Assert.False(vm.HasMoreComments);
    }

    [Fact]
    public async Task LoadMore_AfterTheLastPageDoesNotFetch()
    {
        var client = new StubHackerNewsClient();
        client.AddItem(Comment(1, "only"));
        var story = new HnItem { Id = 100, Type = "story", Kids = [1] };
        var vm = new StoryDetailViewModel(client, new NoopBrowser(), new FixedSettings(), new MutableTimeProvider());

        await vm.InitializeAsync(story);
        var callsAfterInit = client.GetItemsCallCount;

        await vm.LoadMoreCommentsCommand.ExecuteAsync(null);

        Assert.Equal(callsAfterInit, client.GetItemsCallCount);
        Assert.Single(vm.Comments);
    }

    [Fact]
    public async Task Refresh_ReloadsTheThreadFromTheFirstPage()
    {
        var client = new StubHackerNewsClient();
        client.AddItem(Comment(1, "before"));
        var story = new HnItem { Id = 100, Type = "story", Kids = [1] };
        var vm = new StoryDetailViewModel(client, new NoopBrowser(), new FixedSettings(), new MutableTimeProvider());
        await vm.InitializeAsync(story);

        client.AddItem(Comment(1, "after"));
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(["after"], vm.Comments.Select(c => c.Text));
    }

    [Fact]
    public async Task StoryText_IsConvertedToPlainText()
    {
        var client = new StubHackerNewsClient();
        var story = new HnItem { Id = 100, Type = "story", Text = "Ask HN:<p>What&#x27;s up?" };
        var vm = new StoryDetailViewModel(client, new NoopBrowser(), new FixedSettings(), new MutableTimeProvider());

        await vm.InitializeAsync(story);

        Assert.True(vm.HasStoryText);
        Assert.Equal("Ask HN:\n\nWhat's up?", vm.StoryText);
    }

    [Fact]
    public async Task NetworkFailure_SetsErrorAndNotifies()
    {
        var client = new StubHackerNewsClient { Failure = new HttpRequestException("offline") };
        var notifier = new StubUserNotifier();
        var vm = new StoryDetailViewModel(client, new NoopBrowser(), new FixedSettings(), new MutableTimeProvider(), notifier);

        await vm.InitializeAsync(new HnItem { Id = 100, Type = "story", Title = "T", Kids = [1] });

        Assert.NotNull(vm.ErrorMessage);
        Assert.Equal("story:100", Assert.Single(notifier.Shown).Key);

        client.Failure = null;
        client.AddItem(Comment(1, "back online"));
        await notifier.Shown["story:100"].Retry!();

        Assert.Null(vm.ErrorMessage);
        Assert.Equal(["back online"], vm.Comments.Select(c => c.Text));
        Assert.Empty(notifier.Shown);
    }

    [Fact]
    public async Task OpenUrl_ExternalLink_GoesToTheBrowser()
    {
        var browser = new StubBrowserLauncher();
        var nav = new RecordingNavigator();
        var vm = new StoryDetailViewModel(new StubHackerNewsClient(), browser, new FixedSettings { BrowserMode = BrowserMode.InApp }, new MutableTimeProvider(), navigator: nav);

        await vm.OpenUrlCommand.ExecuteAsync(new Uri("https://example.com/a?b=1"));

        Assert.Equal([("https://example.com/a?b=1", BrowserMode.InApp)], browser.Opened);
        Assert.Empty(nav.Stories);
    }

    [Fact]
    public async Task OpenUrl_HackerNewsItem_OpensItsThread()
    {
        var client = new StubHackerNewsClient();
        client.AddItem(new HnItem { Id = 42, Type = "story", Title = "Linked" });
        var browser = new StubBrowserLauncher();
        var nav = new RecordingNavigator();
        var vm = new StoryDetailViewModel(client, browser, new FixedSettings(), new MutableTimeProvider(), navigator: nav);

        await vm.OpenUrlCommand.ExecuteAsync(new Uri("https://news.ycombinator.com/item?id=42"));

        Assert.Equal([42L], nav.Stories.Select(s => s.Id));
        Assert.Empty(browser.Opened);
    }

    [Fact]
    public async Task OpenUrl_UnknownHackerNewsItem_FallsBackToTheBrowser()
    {
        var browser = new StubBrowserLauncher();
        var nav = new RecordingNavigator();
        var vm = new StoryDetailViewModel(new StubHackerNewsClient(), browser, new FixedSettings(), new MutableTimeProvider(), navigator: nav);

        await vm.OpenUrlCommand.ExecuteAsync(new Uri("https://news.ycombinator.com/item?id=7"));

        Assert.Single(browser.Opened);
        Assert.Empty(nav.Stories);
    }
}
