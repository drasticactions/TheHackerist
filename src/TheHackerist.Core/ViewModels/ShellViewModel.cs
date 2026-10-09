using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheHackerist.Abstractions;
using TheHackerist.Models;
using TheHackerist.Services;

namespace TheHackerist.ViewModels;

public sealed partial class ShellViewModel : ObservableObject, INavigator
{
    private readonly IHackerNewsClient _client;
    private readonly IBrowserLauncher _browser;
    private readonly IAppSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly IUserNotifier _notifier;
    private readonly PlatformFeatures _features;

    private int _firstVisibleFeed;
    private int _lastVisibleFeed;

    public ShellViewModel(
        IHackerNewsClient client,
        IBrowserLauncher browser,
        IAppSettings settings,
        TimeProvider? timeProvider = null,
        IUserNotifier? notifier = null,
        PlatformFeatures? features = null)
    {
        _client = client;
        _browser = browser;
        _settings = settings;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _notifier = notifier ?? NullUserNotifier.Instance;
        _features = features ?? new PlatformFeatures();

        Feeds = [.. Enum.GetValues<FeedType>().Select(f => new FeedViewModel(f, _client, _browser, _settings, this, _timeProvider, _notifier))];
        Settings = new SettingsViewModel(settings, _features, _browser);
    }

    public IReadOnlyList<FeedViewModel> Feeds { get; }

    public SettingsViewModel Settings { get; }

    public ObservableCollection<object> Pages { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentFeed))]
    public partial int SelectedFeedIndex { get; set; }

    public FeedViewModel CurrentFeed => Feeds[Math.Clamp(SelectedFeedIndex, 0, Feeds.Count - 1)];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHome))]
    [NotifyPropertyChangedFor(nameof(CurrentStory))]
    [NotifyPropertyChangedFor(nameof(CurrentReader))]
    public partial object? CurrentPage { get; private set; }

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    public bool CanGoBack => Pages.Count > 0;

    public bool IsHome => CurrentPage is null;

    public StoryDetailViewModel? CurrentStory => CurrentPage as StoryDetailViewModel;

    public ReaderViewModel? CurrentReader => CurrentPage as ReaderViewModel;

    public Task InitializeAsync() => ShowFeedsAsync(SelectedFeedIndex, SelectedFeedIndex);

    public Task ShowFeedsAsync(int first, int last)
    {
        _firstVisibleFeed = Math.Clamp(Math.Min(first, last), 0, Feeds.Count - 1);
        _lastVisibleFeed = Math.Clamp(Math.Max(first, last), 0, Feeds.Count - 1);
        return Task.WhenAll(VisibleFeeds().Select(f => f.EnsureLoadedAsync()));
    }

    public async Task GoToStoryAsync(HnItem story)
    {
        if (Pages.Count > 0 && Pages[0] is StoryDetailViewModel { Story.Id: var openId } && openId == story.Id)
        {
            while (Pages.Count > 1)
            {
                Pages.RemoveAt(Pages.Count - 1);
            }

            SyncStackState();
            return;
        }

        var detail = new StoryDetailViewModel(_client, _browser, _settings, _timeProvider, _notifier, this);
        Pages.Clear();
        Push(detail);
        await detail.InitializeAsync(story);
    }

    public void OpenReader(Uri url)
    {
        ReaderViewModel? reader = null;
        reader = new ReaderViewModel(url, _browser, _features, _notifier, () => Close(reader!));
        Push(reader);
    }

    partial void OnSelectedFeedIndexChanged(int value) => _ = ShowFeedsAsync(value, value);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task RefreshAsync() => CurrentPage switch
    {
        StoryDetailViewModel story => story.RefreshCommand.ExecuteAsync(null),
        null => Task.WhenAll(VisibleFeeds().Select(f => f.RefreshCommand.ExecuteAsync(null))),
        _ => Task.CompletedTask,
    };

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void GoBack()
    {
        if (Pages.Count == 0)
        {
            return;
        }

        Pages.RemoveAt(Pages.Count - 1);
        SyncStackState();
    }

    private void Close(object page)
    {
        if (Pages.Remove(page))
        {
            SyncStackState();
        }
    }

    private IEnumerable<FeedViewModel> VisibleFeeds() =>
        Feeds.Skip(_firstVisibleFeed).Take(_lastVisibleFeed - _firstVisibleFeed + 1);

    private void Push(object page)
    {
        Pages.Add(page);
        SyncStackState();
    }

    private void SyncStackState()
    {
        CurrentPage = Pages.Count > 0 ? Pages[^1] : null;
        OnPropertyChanged(nameof(CanGoBack));
    }
}
