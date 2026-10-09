using TheHackerist.Helpers;

namespace TheHackerist.Tests;

public class RelativeTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(30, "just now")]
    [InlineData(90, "1m ago")]
    [InlineData(3599, "59m ago")]
    [InlineData(3600, "1h ago")]
    [InlineData(86399, "23h ago")]
    [InlineData(86400, "1d ago")]
    [InlineData(86400 * 29, "29d ago")]
    [InlineData(86400 * 45, "1mo ago")]
    [InlineData(86400 * 400, "1y ago")]
    public void Format_ProducesCompactStrings(int secondsAgo, string expected)
    {
        var timestamp = Now.AddSeconds(-secondsAgo);
        Assert.Equal(expected, RelativeTime.Format(timestamp, Now));
    }

    [Fact]
    public void FutureTimestamps_ClampToJustNow()
    {
        Assert.Equal("just now", RelativeTime.Format(Now.AddMinutes(5), Now));
    }
}
