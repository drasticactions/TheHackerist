using System.Windows.Input;
using Avalonia.Controls;

namespace TheHackerist.Views;

internal static class ScrollPaging
{
    private const double Threshold = 0.5;

    public static void LoadMoreNearEnd(Control list, Func<ICommand?> command)
    {
        list.AddHandler(ScrollViewer.ScrollChangedEvent, (_, e) =>
        {
            if (e.Source is not ScrollViewer scroller || e.OffsetDelta.Y <= 0)
            {
                return;
            }

            var remaining = scroller.Extent.Height - scroller.Offset.Y - scroller.Viewport.Height;
            if (remaining <= scroller.Viewport.Height * Threshold && command() is { } c && c.CanExecute(null))
            {
                c.Execute(null);
            }
        });
    }
}
