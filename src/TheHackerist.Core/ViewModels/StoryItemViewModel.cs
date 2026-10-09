using TheHackerist.Helpers;
using TheHackerist.Models;

namespace TheHackerist.ViewModels;

public sealed class StoryItemViewModel
{
    public StoryItemViewModel(HnItem item, int rank, DateTimeOffset now)
    {
        Item = item;
        Rank = rank;
        TimeAgo = RelativeTime.Format(item.TimestampUtc, now);
    }

    public HnItem Item { get; }

    public int Rank { get; }

    public string Title => Item.Title ?? "(untitled)";

    public string? Domain => Item.Domain;

    public bool HasDomain => Item.Domain is not null;

    public string? Url => Item.Url;

    public bool HasUrl => !string.IsNullOrEmpty(Item.Url);

    public int Score => Item.Score;

    public string Author => Item.By ?? "unknown";

    public int CommentCount => Item.Descendants;

    public string TimeAgo { get; }

    public string Byline => $"{Author} · {TimeAgo} · {CommentCount} comments";

    public string StatsLine => $"▲ {Score}";
}
