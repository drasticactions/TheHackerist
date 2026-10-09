using TheHackerist.ViewModels;

namespace TheHackerist.Tests;

public class ReaderViewModelTests
{
    [Fact]
    public void BackAndForward_FollowTheWebHistory()
    {
        var reader = new ReaderViewModel(new Uri("https://example.com/post"));
        var actions = new List<ReaderAction>();
        reader.ActionRequested += (_, a) => actions.Add(a);

        Assert.False(reader.GoBackCommand.CanExecute(null));
        Assert.False(reader.GoForwardCommand.CanExecute(null));

        var raised = false;
        reader.GoBackCommand.CanExecuteChanged += (_, _) => raised = true;
        reader.CanGoBack = true;

        Assert.True(raised);
        Assert.True(reader.GoBackCommand.CanExecute(null));

        reader.GoBackCommand.Execute(null);
        reader.ReloadCommand.Execute(null);

        Assert.Equal([ReaderAction.Back, ReaderAction.Reload], actions);
    }
}
