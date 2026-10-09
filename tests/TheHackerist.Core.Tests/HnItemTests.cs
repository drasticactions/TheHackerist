using System.Text.Json;
using TheHackerist.Models;
using TheHackerist.Serialization;

namespace TheHackerist.Tests;

public class HnItemTests
{
    [Theory]
    [InlineData("https://www.example.com/story", "example.com")]
    [InlineData("https://blog.example.org/post", "blog.example.org")]
    [InlineData(null, null)]
    [InlineData("not a url", null)]
    public void Domain_StripsWwwAndHandlesInvalidUrls(string? url, string? expected)
    {
        var item = new HnItem { Url = url };
        Assert.Equal(expected, item.Domain);
    }

    [Fact]
    public void Deserializes_RealApiShape()
    {
        const string json = """
            {
              "by": "dhouston",
              "descendants": 71,
              "id": 8863,
              "kids": [9224, 8917],
              "score": 104,
              "time": 1175714200,
              "title": "My YC app: Dropbox - Throw away your USB drive",
              "type": "story",
              "url": "http://www.getdropbox.com/u/2/screencast.html"
            }
            """;

        var item = JsonSerializer.Deserialize(json, HnJsonContext.Default.HnItem);

        Assert.NotNull(item);
        Assert.Equal(8863, item.Id);
        Assert.Equal("dhouston", item.By);
        Assert.Equal(104, item.Score);
        Assert.Equal(71, item.Descendants);
        Assert.Equal([9224, 8917], item.Kids);
        Assert.Equal("getdropbox.com", item.Domain);
        Assert.True(item.IsVisible);
        Assert.Equal(new DateTimeOffset(2007, 4, 4, 19, 16, 40, TimeSpan.Zero), item.TimestampUtc);
    }

    [Fact]
    public void DeletedOrDeadItems_AreNotVisible()
    {
        Assert.False(new HnItem { Deleted = true }.IsVisible);
        Assert.False(new HnItem { Dead = true }.IsVisible);
    }
}
