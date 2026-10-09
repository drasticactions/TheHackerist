using Avalonia.Controls;
using TheHackerist.Abstractions;
using TheHackerist.Models;
using TheHackerist.Services;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.ViewModels;
using TheHackerist.Views;

namespace TheHackerist.UI.Tests;

public sealed class ShellFixture
{
    public ShellFixture(double width = 1280, double height = 800, bool supportsInAppBrowser = true, IAppSettings? settings = null, Exception? failure = null)
    {
        Client = new StubHackerNewsClient();
        Client.Feeds[FeedType.Top] = [1, 2, 3];
        Client.Feeds[FeedType.New] = [4];
        Client.AddItem(Story(1, "First story", "https://example.com/one", kids: [11, 12]));
        Client.AddItem(Story(2, "Second story", "https://example.org/two"));
        Client.AddItem(Story(3, "Ask HN: a text post", null));
        Client.AddItem(Story(4, "Newest story", "https://example.net/new"));
        Client.AddItem(Comment(11, "Top comment with <a href=\"https://example.com/linked\">a link</a>", 111));
        Client.AddItem(Comment(111, "A reply", 1111));
        Client.AddItem(Comment(1111, "A deeper reply"));
        Client.AddItem(Comment(12, "Second top comment"));

        Client.Failure = failure;
        Features = new PlatformFeatures(supportsInAppBrowser);
        Settings = settings ?? new StubAppSettings { BrowserMode = BrowserMode.InApp };
        Notifier = new DelegatingUserNotifier();
        Browser = new RecordingLauncher(() => Shell!);
        Shell = new ShellViewModel(Client, Browser, Settings, new MutableTimeProvider(), Notifier, Features);
        View = new MainView { DataContext = Shell, Notifier = Notifier };
        Window = TestHost.Show(View, width, height);
    }

    public StubHackerNewsClient Client { get; }

    public PlatformFeatures Features { get; }

    public IAppSettings Settings { get; }

    public DelegatingUserNotifier Notifier { get; }

    public RecordingLauncher Browser { get; }

    public ShellViewModel Shell { get; }

    public MainView View { get; }

    public Window Window { get; }

    public static HnItem Story(long id, string title, string? url, long[]? kids = null) =>
        new() { Id = id, Type = "story", Title = title, Url = url, By = "author", Time = 1753600000, Score = 10, Kids = kids, Descendants = kids?.Length ?? 0 };

    public static HnItem Comment(long id, string text, params long[] kids) =>
        new() { Id = id, Type = "comment", By = $"user{id}", Time = 1753600000, Text = text, Kids = kids.Length > 0 ? kids : null };

    public void Settle() => TestHost.Settle(Window);

    public bool Pump(Func<bool> until, int ms = 3000) => TestHost.Pump(Window, until, ms);

    public sealed class RecordingLauncher(Func<ShellViewModel> shell) : IBrowserLauncher
    {
        public List<(string Url, BrowserMode Mode)> Opened { get; } = [];

        public Task OpenAsync(string url, BrowserMode mode)
        {
            Opened.Add((url, mode));
            if (mode == BrowserMode.InApp && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                shell().OpenReader(uri);
            }

            return Task.CompletedTask;
        }
    }
}
