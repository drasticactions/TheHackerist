using TheHackerist.Services;
using TheHackerist.Tests.TestDoubles;

namespace TheHackerist.Tests;

public class HackerNewsClientTests
{
    private static (HackerNewsClient Client, FakeHttpMessageHandler Handler, MutableTimeProvider Time) CreateClient()
    {
        var handler = new FakeHttpMessageHandler();
        var time = new MutableTimeProvider();
        var client = new HackerNewsClient(new HttpClient(handler), time);
        return (client, handler, time);
    }

    private static string ItemJson(long id, string title = "A story") =>
        $$"""{"id": {{id}}, "type": "story", "title": "{{title}}", "by": "user", "time": 1753600000, "score": 10}""";

    [Fact]
    public async Task GetFeedIdsAsync_ReturnsIdsInOrder()
    {
        var (client, handler, _) = CreateClient();
        handler.SetResponse("/v0/topstories.json", "[3, 1, 2]");

        var ids = await client.GetFeedIdsAsync(FeedType.Top, TestContext.Current.CancellationToken);

        Assert.Equal([3, 1, 2], ids);
    }

    [Fact]
    public async Task GetItemAsync_CachesWithinTtl()
    {
        var (client, handler, time) = CreateClient();
        handler.SetResponse("/v0/item/1.json", ItemJson(1));

        var first = await client.GetItemAsync(1, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(1));
        var second = await client.GetItemAsync(1, TestContext.Current.CancellationToken);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(1, handler.RequestCounts["/v0/item/1.json"]);
    }

    [Fact]
    public async Task GetItemAsync_RefetchesAfterTtlExpires()
    {
        var (client, handler, time) = CreateClient();
        handler.SetResponse("/v0/item/1.json", ItemJson(1));

        await client.GetItemAsync(1, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(6));
        await client.GetItemAsync(1, TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.RequestCounts["/v0/item/1.json"]);
    }

    [Fact]
    public async Task GetItemAsync_ReturnsNullForMissingItems()
    {
        var (client, _, _) = CreateClient();

        var item = await client.GetItemAsync(99, TestContext.Current.CancellationToken);

        Assert.Null(item);
    }

    [Fact]
    public async Task GetItemsAsync_PreservesOrderAndSkipsMissingAndDead()
    {
        var (client, handler, _) = CreateClient();
        handler.SetResponse("/v0/item/1.json", ItemJson(1, "first"));
        handler.SetResponse("/v0/item/2.json", """{"id": 2, "type": "story", "dead": true}""");
        handler.SetResponse("/v0/item/3.json", ItemJson(3, "third"));

        var items = await client.GetItemsAsync([1, 2, 3, 4], TestContext.Current.CancellationToken);

        Assert.Equal(["first", "third"], items.Select(i => i.Title));
    }
}
