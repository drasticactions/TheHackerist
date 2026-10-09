using TheHackerist.Models;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.ViewModels;

namespace TheHackerist.Tests;

public class CommentThreadTests
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

    private static StoryDetailViewModel CreateViewModel(StubHackerNewsClient client) =>
        new(client, new StubBrowserLauncher(), new StubAppSettings(), new MutableTimeProvider());

    private static (StubHackerNewsClient Client, HnItem Story) ThreeLevelThread()
    {
        var client = new StubHackerNewsClient();
        client.AddItem(Comment(1, "first", 11));
        client.AddItem(Comment(11, "reply", 111));
        client.AddItem(Comment(111, "deep reply"));
        client.AddItem(Comment(2, "second", 21));
        client.AddItem(Comment(21, "another reply"));

        return (client, new HnItem { Id = 100, Type = "story", Title = "T", Kids = [1, 2] });
    }

    [Fact]
    public async Task Thread_IsFetchedOneBatchPerLevel()
    {
        var (client, story) = ThreeLevelThread();
        var vm = CreateViewModel(client);

        await vm.InitializeAsync(story);

        Assert.Equal(3, client.GetItemsCallCount);
        Assert.Equal(5, vm.Comments.Count);
    }

    [Fact]
    public async Task Thread_KeepsDepthFirstOrderAndDepths()
    {
        var (client, story) = ThreeLevelThread();
        var vm = CreateViewModel(client);

        await vm.InitializeAsync(story);

        Assert.Equal(["first", "reply", "deep reply", "second", "another reply"], vm.Comments.Select(c => c.Text));
        Assert.Equal([0, 1, 2, 0, 1], vm.Comments.Select(c => c.Depth));
    }

    [Fact]
    public async Task Comments_CountTheirLoadedDescendants()
    {
        var (client, story) = ThreeLevelThread();
        var vm = CreateViewModel(client);

        await vm.InitializeAsync(story);

        Assert.Equal(2, vm.Comments[0].DescendantCount);
        Assert.Equal(1, vm.Comments[1].DescendantCount);
        Assert.Equal(0, vm.Comments[2].DescendantCount);
        Assert.True(vm.Comments[0].HasReplies);
        Assert.False(vm.Comments[2].HasReplies);
    }

    [Fact]
    public async Task ToggleCollapse_HidesTheWholeSubtreeAndNothingElse()
    {
        var (client, story) = ThreeLevelThread();
        var vm = CreateViewModel(client);
        await vm.InitializeAsync(story);

        vm.ToggleCollapseCommand.Execute(vm.Comments[0]);

        Assert.True(vm.Comments[0].IsCollapsed);
        Assert.True(vm.Comments[0].IsVisible);
        Assert.False(vm.Comments[1].IsVisible);
        Assert.False(vm.Comments[2].IsVisible);
        Assert.True(vm.Comments[3].IsVisible);
        Assert.True(vm.Comments[4].IsVisible);
    }

    [Fact]
    public async Task ToggleCollapse_Twice_RestoresVisibility()
    {
        var (client, story) = ThreeLevelThread();
        var vm = CreateViewModel(client);
        await vm.InitializeAsync(story);

        vm.ToggleCollapseCommand.Execute(vm.Comments[0]);
        vm.ToggleCollapseCommand.Execute(vm.Comments[0]);

        Assert.False(vm.Comments[0].IsCollapsed);
        Assert.All(vm.Comments, c => Assert.True(c.IsVisible));
    }

    [Fact]
    public async Task Expanding_KeepsAlreadyCollapsedSubThreadsCollapsed()
    {
        var (client, story) = ThreeLevelThread();
        var vm = CreateViewModel(client);
        await vm.InitializeAsync(story);

        vm.ToggleCollapseCommand.Execute(vm.Comments[1]);
        vm.ToggleCollapseCommand.Execute(vm.Comments[0]);
        vm.ToggleCollapseCommand.Execute(vm.Comments[0]);

        Assert.True(vm.Comments[1].IsVisible);
        Assert.True(vm.Comments[1].IsCollapsed);
        Assert.False(vm.Comments[2].IsVisible);
    }

    [Fact]
    public async Task ToggleCollapse_IgnoresLeafComments()
    {
        var (client, story) = ThreeLevelThread();
        var vm = CreateViewModel(client);
        await vm.InitializeAsync(story);

        vm.ToggleCollapseCommand.Execute(vm.Comments[2]);

        Assert.False(vm.Comments[2].IsCollapsed);
    }

    [Fact]
    public async Task CollapseGlyph_ShowsHiddenReplyCount()
    {
        var (client, story) = ThreeLevelThread();
        var vm = CreateViewModel(client);
        await vm.InitializeAsync(story);

        Assert.Equal("–", vm.Comments[0].CollapseGlyph);

        vm.ToggleCollapseCommand.Execute(vm.Comments[0]);

        Assert.Equal("+2", vm.Comments[0].CollapseGlyph);
    }
}
