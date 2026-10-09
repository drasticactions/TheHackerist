using AvaEnyo;
using AvaEnyo.Controls;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Headless.XUnit;
using TheHackerist.Abstractions;
using TheHackerist.Services;
using TheHackerist.Views;

namespace TheHackerist.UI.Tests;

public class ShellTests
{
    private static ListBox Stories(ShellFixture f) => TestHost.Find<ListBox>(f.View, "StoryList");

    private static SlidingView Panel(ShellFixture f, string name) => TestHost.Find<SlidingView>(f.View, name);

    private static SlidingPane Sliding(ShellFixture f) => TestHost.Find<SlidingPane>(f.View, "Sliding");

    [AvaloniaFact]
    public async Task Startup_ShowsTheTopFeedsStories()
    {
        var f = new ShellFixture();
        await f.Shell.InitializeAsync();
        f.Settle();

        Assert.Equal(3, Stories(f).ItemCount);
        Assert.Contains(TestHost.All<TextBlock>(f.View), t => t.Text == "First story" && t.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task TappingAFeed_ShowsItsStories()
    {
        var f = new ShellFixture();
        await f.Shell.InitializeAsync();
        f.Settle();

        var feeds = TestHost.Find<ListBox>(f.View, "FeedList");
        TestHost.Click(f.Window, feeds.ContainerFromIndex((int)FeedType.New)!);

        Assert.Equal((int)FeedType.New, f.Shell.SelectedFeedIndex);
        Assert.True(f.Pump(() => Stories(f).ItemCount == 1));
        Assert.Same(Panel(f, "StoriesPanel"), Sliding(f).SelectedView);
    }

    [AvaloniaFact]
    public async Task SelectingAStory_FillsTheDetailAndSlidesToIt()
    {
        var f = new ShellFixture();
        await f.Shell.InitializeAsync();
        f.Settle();

        TestHost.Click(f.Window, Stories(f).ContainerFromIndex(0)!);

        Assert.True(f.Pump(() => f.Shell.CurrentStory is { Comments.Count: 4 }));
        f.Settle();
        Assert.Same(Panel(f, "DetailPanel"), Sliding(f).SelectedView);
        Assert.Contains(TestHost.All<StoryDetailView>(f.View), v => v.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task NarrowWindow_SlidesToTheDetailAndBackReturnsToTheStories()
    {
        var f = new ShellFixture(width: 400, height: 800);
        await f.Shell.InitializeAsync();
        f.Settle();
        Assert.True(Sliding(f).IsNarrow);

        f.Shell.CurrentFeed.SelectedStory = f.Shell.CurrentFeed.Stories[1];
        Assert.True(f.Pump(() => f.Shell.CurrentStory is not null));
        f.Settle();

        var detail = Panel(f, "DetailPanel");
        Assert.Same(detail, Sliding(f).SelectedView);
        Assert.Equal(400, detail.Bounds.Width, 1);

        EnyoBack.RequestBack(f.Window);
        f.Settle();

        Assert.Same(Panel(f, "StoriesPanel"), Sliding(f).SelectedView);
    }

    [AvaloniaFact]
    public async Task TappingADomainLink_OpensTheArticleWithoutSelectingTheRow()
    {
        var f = new ShellFixture();
        f.Settings.BrowserMode = BrowserMode.External;
        await f.Shell.InitializeAsync();
        f.Settle();

        var row = Stories(f).ContainerFromIndex(0)!;
        var domain = TestHost.All<HyperlinkButton>(row).First(b => b.Classes.Contains("domain"));
        TestHost.Click(f.Window, domain);

        Assert.Equal([("https://example.com/one", BrowserMode.External)], f.Browser.Opened);
        Assert.Null(f.Shell.CurrentFeed.SelectedStory);
        Assert.Null(f.Shell.CurrentPage);
    }

    [AvaloniaFact]
    public async Task ScrollingToTheEnd_LoadsTheNextPageOnce()
    {
        var f = new ShellFixture(height: 500);
        var ids = Enumerable.Range(100, 45).Select(i => (long)i).ToArray();
        f.Client.Feeds[FeedType.Top] = ids;
        foreach (var id in ids)
        {
            f.Client.AddItem(ShellFixture.Story(id, $"Story {id}", $"https://example.com/{id}"));
        }

        await f.Shell.InitializeAsync();
        f.Settle();
        Assert.Equal(30, Stories(f).ItemCount);
        var batchesBefore = f.Client.GetItemsCallCount;

        var scroller = TestHost.All<ScrollViewer>(Stories(f)).First();
        scroller.Offset = new Avalonia.Vector(0, scroller.Extent.Height);
        Assert.True(f.Pump(() => Stories(f).ItemCount == 45));
        scroller.Offset = new Avalonia.Vector(0, scroller.Extent.Height);
        f.Settle();

        Assert.Equal(batchesBefore + 1, f.Client.GetItemsCallCount);
    }

    [AvaloniaFact]
    public async Task NarrowWindow_TappingTheSelectedFeedAndStory_StillSlidesForward()
    {
        var f = new ShellFixture(width: 400, height: 800);
        await f.Shell.InitializeAsync();
        f.Settle();
        Sliding(f).SelectView(Panel(f, "FeedsPanel"), immediate: true);
        f.Settle();

        var feeds = TestHost.Find<ListBox>(f.View, "FeedList");
        TestHost.Click(f.Window, feeds.ContainerFromIndex((int)FeedType.Top)!);
        Assert.Same(Panel(f, "StoriesPanel"), Sliding(f).SelectedView);

        TestHost.Click(f.Window, Stories(f).ContainerFromIndex(0)!);
        Assert.True(f.Pump(() => f.Shell.CurrentStory is not null));
        Assert.Same(Panel(f, "DetailPanel"), Sliding(f).SelectedView);

        EnyoBack.RequestBack(f.Window);
        f.Settle();
        Assert.Same(Panel(f, "StoriesPanel"), Sliding(f).SelectedView);

        var row = Stories(f).ContainerFromIndex(0)!;
        TestHost.Click(f.Window, row, new Point(row.Bounds.Width - 20, 12));
        f.Settle();
        Assert.Same(Panel(f, "DetailPanel"), Sliding(f).SelectedView);
    }
}
