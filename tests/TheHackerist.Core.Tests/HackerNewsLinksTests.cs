using TheHackerist.Helpers;

namespace TheHackerist.Tests;

public class HackerNewsLinksTests
{
    [Theory]
    [InlineData("https://news.ycombinator.com/item?id=123", 123L)]
    [InlineData("http://NEWS.ycombinator.com/item?id=5&p=2", 5L)]
    [InlineData("https://news.ycombinator.com/item?id=abc", null)]
    [InlineData("https://news.ycombinator.com/user?id=pg", null)]
    [InlineData("https://example.com/item?id=1", null)]
    public void TryGetItemId(string url, long? expected) =>
        Assert.Equal(expected, HackerNewsLinks.TryGetItemId(new Uri(url)));
}
