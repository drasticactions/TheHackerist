using AvaEnyo.Services;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using TheHackerist.Views;

namespace TheHackerist.UI.Tests;

public class ErrorTests
{
    [AvaloniaFact]
    public async Task FailingFeed_ShowsOneDashboardAndAnInlineMessage_AndATapRetries()
    {
        var f = new ShellFixture(failure: new HttpRequestException("offline"));
        var dashboards = new TestNotificationService();
        f.Notifier.Target = new EnyoUserNotifier(dashboards);

        await f.Shell.InitializeAsync();
        await f.Shell.CurrentFeed.RefreshCommand.ExecuteAsync(null);
        f.Settle();

        var notification = Assert.Single(dashboards.Notifications);
        Assert.Equal("feed:Top", notification.Id);

        dashboards.Dismiss(notification.Id);
        f.Settle();
        Assert.True(TestHost.Find<StackPanel>(f.View, "EmptyState").IsEffectivelyVisible);
        Assert.True(TestHost.Find<Button>(f.View, "EmptyRefreshButton").IsEffectivelyVisible);

        await f.Shell.CurrentFeed.RefreshCommand.ExecuteAsync(null);
        f.Settle();
        notification = Assert.Single(dashboards.Notifications);

        f.Client.Failure = null;
        dashboards.Activate(notification);
        Assert.True(f.Pump(() => f.Shell.CurrentFeed.Stories.Count == 3));
        f.Settle();

        Assert.Empty(dashboards.Notifications);
        Assert.False(TestHost.Find<StackPanel>(f.View, "EmptyState").IsEffectivelyVisible);
    }
}
