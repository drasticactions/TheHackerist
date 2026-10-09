namespace TheHackerist.Services;

public enum FeedType
{
    Top,
    New,
    Best,
    Ask,
    Show,
    Jobs,
}

public static class FeedTypeExtensions
{
    public static string ToEndpoint(this FeedType feed) => feed switch
    {
        FeedType.Top => "topstories",
        FeedType.New => "newstories",
        FeedType.Best => "beststories",
        FeedType.Ask => "askstories",
        FeedType.Show => "showstories",
        FeedType.Jobs => "jobstories",
        _ => throw new ArgumentOutOfRangeException(nameof(feed), feed, null),
    };

    public static string ToDisplayName(this FeedType feed) => feed switch
    {
        FeedType.Top => "Top",
        FeedType.New => "New",
        FeedType.Best => "Best",
        FeedType.Ask => "Ask HN",
        FeedType.Show => "Show HN",
        FeedType.Jobs => "Jobs",
        _ => feed.ToString(),
    };
}
