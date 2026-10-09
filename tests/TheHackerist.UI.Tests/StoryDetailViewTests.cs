using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using TheHackerist.Abstractions;
using TheHackerist.Models;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.ViewModels;
using TheHackerist.Views;

namespace TheHackerist.UI.Tests;

public class StoryDetailViewTests
{
    private static HnItem Comment(long id, string text, params long[] kids) => new()
    {
        Id = id,
        Type = "comment",
        By = $"user{id}",
        Time = 1753600000,
        Text = text,
        Kids = kids.Length > 0 ? kids : null,
    };

    private static async Task<(Window Window, StoryDetailView View, StoryDetailViewModel Vm, StubBrowserLauncher Browser)> ShowThread(params HnItem[] comments)
    {
        var client = new StubHackerNewsClient();
        foreach (var c in comments)
        {
            client.AddItem(c);
        }

        var browser = new StubBrowserLauncher();
        var vm = new StoryDetailViewModel(client, browser, new StubAppSettings { BrowserMode = BrowserMode.External }, new MutableTimeProvider());
        var roots = comments.Where(c => !comments.Any(p => p.Kids?.Contains(c.Id) == true)).Select(c => c.Id).ToArray();
        await vm.InitializeAsync(new HnItem { Id = 100, Type = "story", Title = "Story", Kids = roots });

        var view = new StoryDetailView { DataContext = vm };
        var window = TestHost.Show(view);
        return (window, view, vm, browser);
    }

    private static SelectableTextBlock BodyOf(StoryDetailView view, int index) =>
        TestHost.All<SelectableTextBlock>(view).Where(t => t.Classes.Contains("comment-body")).ElementAt(index);

    [AvaloniaFact]
    public async Task TappingALinkInAComment_OpensItWithoutCollapsing()
    {
        var (window, view, vm, browser) = await ShowThread(
            Comment(1, "See <a href=\"https://example.com/full/path\">https://example.com/full/...</a> here", 2),
            Comment(2, "reply"));

        var body = BodyOf(view, 0);
        var at = body.TextLayout.HitTestTextPosition("See ".Length + 3);
        TestHost.Click(window, body, new Point(at.X + 1, at.Y + at.Height / 2));

        Assert.Equal([("https://example.com/full/path", BrowserMode.External)], browser.Opened);
        Assert.False(vm.Comments[0].IsCollapsed);
    }

    [AvaloniaFact]
    public async Task TappingAHeader_HidesAndRestoresTheReplies_KeepingCollapsedSubThreads()
    {
        var (window, view, vm, _) = await ShowThread(
            Comment(1, "root", 2, 4),
            Comment(2, "child", 3),
            Comment(3, "grandchild"),
            Comment(4, "second child"));

        bool Shown(int index) => TestHost.All<SelectableTextBlock>(view)
            .Any(t => t.Classes.Contains("comment-body") && t.IsEffectivelyVisible && t.DataContext == vm.Comments[index]);

        Border Header(int index) => TestHost.All<Border>(view)
            .First(b => b.Name == "CommentHeader" && b.DataContext == vm.Comments[index]);

        TestHost.Click(window, Header(1));
        Assert.False(Shown(2));

        TestHost.Click(window, Header(0));
        Assert.False(Shown(1));
        Assert.False(Shown(3));

        TestHost.Click(window, Header(0));
        Assert.Equal([true, true, false, true], vm.Comments.Select(c => c.IsVisible));
        TestHost.Settle(window);
        Assert.True(Shown(1), "child");
        Assert.False(Shown(2));
        Assert.True(Shown(3), "second child");
    }
}
