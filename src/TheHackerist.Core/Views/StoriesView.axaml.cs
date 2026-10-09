using Avalonia.Controls;
using TheHackerist.ViewModels;

namespace TheHackerist.Views;

public partial class StoriesView : UserControl
{
    public StoriesView()
    {
        InitializeComponent();
        ScrollPaging.LoadMoreNearEnd(StoryList, () => (DataContext as FeedViewModel)?.LoadMoreCommand);
    }
}
