using Avalonia.Controls;
using Avalonia.Input;
using TheHackerist.ViewModels;

namespace TheHackerist.Views;

public partial class StoryDetailView : UserControl
{
    public StoryDetailView()
    {
        InitializeComponent();
        ScrollPaging.LoadMoreNearEnd(Scroller, () => (DataContext as StoryDetailViewModel)?.LoadMoreCommentsCommand);
    }

    private void OnCommentHeaderTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: CommentViewModel comment } && DataContext is StoryDetailViewModel story)
        {
            story.ToggleCollapseCommand.Execute(comment);
            e.Handled = true;
        }
    }
}
