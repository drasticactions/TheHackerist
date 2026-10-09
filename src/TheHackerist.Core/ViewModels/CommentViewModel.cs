using CommunityToolkit.Mvvm.ComponentModel;
using TheHackerist.Helpers;
using TheHackerist.Models;

namespace TheHackerist.ViewModels;

public sealed partial class CommentViewModel : ObservableObject
{
    public CommentViewModel(HnItem item, int depth, DateTimeOffset now, int descendantCount = 0)
    {
        Item = item;
        Depth = depth;
        DescendantCount = descendantCount;
        Author = item.By ?? "unknown";
        TimeAgo = RelativeTime.Format(item.TimestampUtc, now);
        Text = HtmlText.ToPlainText(item.Text);
        Blocks = HnText.Parse(item.Text);
    }

    public HnItem Item { get; }

    public int Depth { get; }

    public int DescendantCount { get; }

    public string Author { get; }

    public string TimeAgo { get; }

    public string Text { get; }

    public IReadOnlyList<HnTextBlock> Blocks { get; }

    public string HeaderLine => $"{Author} · {TimeAgo}";

    public bool HasReplies => DescendantCount > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollapseGlyph))]
    public partial bool IsCollapsed { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    public string CollapseGlyph => IsCollapsed ? $"+{DescendantCount}" : "–";
}
