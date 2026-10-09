namespace TheHackerist.Helpers;

public static class RelativeTime
{
    public static string Format(DateTimeOffset timestamp, DateTimeOffset now)
    {
        var delta = now - timestamp;
        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }

        return delta switch
        {
            { TotalMinutes: < 1 } => "just now",
            { TotalHours: < 1 } => $"{(int)delta.TotalMinutes}m ago",
            { TotalDays: < 1 } => $"{(int)delta.TotalHours}h ago",
            { TotalDays: < 30 } => $"{(int)delta.TotalDays}d ago",
            { TotalDays: < 365 } => $"{(int)(delta.TotalDays / 30)}mo ago",
            _ => $"{(int)(delta.TotalDays / 365)}y ago",
        };
    }
}
