using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheHackerist.Abstractions;
using TheHackerist.Services;

namespace TheHackerist.ViewModels;

public sealed partial class FeedViewModel : ObservableObject
{
    private const int PageSize = 30;

    private readonly IHackerNewsClient _client;
    private readonly IBrowserLauncher _browser;
    private readonly IAppSettings _settings;
    private readonly INavigator _navigator;
    private readonly TimeProvider _timeProvider;
    private readonly IUserNotifier _notifier;

    private IReadOnlyList<long> _feedIds = [];
    private int _loadedCount;

    public FeedViewModel(
        FeedType feed,
        IHackerNewsClient client,
        IBrowserLauncher browser,
        IAppSettings settings,
        INavigator navigator,
        TimeProvider? timeProvider = null,
        IUserNotifier? notifier = null)
    {
        Feed = feed;
        _client = client;
        _browser = browser;
        _settings = settings;
        _navigator = navigator;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _notifier = notifier ?? NullUserNotifier.Instance;
        Title = feed.ToDisplayName();
    }

    public FeedType Feed { get; }

    public string Title { get; }

    public ObservableCollection<StoryItemViewModel> Stories { get; } = [];

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    [ObservableProperty]
    public partial bool HasMore { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial StoryItemViewModel? SelectedStory { get; set; }

    private string NotificationKey => $"feed:{Feed}";

    public bool IsEmpty => Stories.Count == 0;

    public override string ToString() => Title;

    public async Task EnsureLoadedAsync()
    {
        if (Stories.Count == 0 && !IsRefreshing)
        {
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            ErrorMessage = null;
            _feedIds = await _client.GetFeedIdsAsync(Feed);
            SelectedStory = null;
            Stories.Clear();
            _loadedCount = 0;
            await LoadPageAsync();
            _notifier.Clear(NotificationKey);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ReportError();
        }
        finally
        {
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadMoreAsync()
    {
        if (IsLoadingMore || IsRefreshing || !HasMore)
        {
            return;
        }

        try
        {
            IsLoadingMore = true;
            await LoadPageAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ReportError();
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task OpenStoryAsync(StoryItemViewModel story) => _navigator.GoToStoryAsync(story.Item);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task OpenArticleAsync(StoryItemViewModel story)
    {
        if (story.HasUrl)
        {
            await _browser.OpenAsync(story.Url!, _settings.BrowserMode);
        }
        else
        {
            await _navigator.GoToStoryAsync(story.Item);
        }
    }

    partial void OnSelectedStoryChanged(StoryItemViewModel? value)
    {
        if (value is not null)
        {
            _ = OpenStoryCommand.ExecuteAsync(value);
        }
    }

    private void ReportError()
    {
        ErrorMessage = "Couldn't reach Hacker News.";
        _notifier.ShowError(NotificationKey, $"{Title}: couldn't reach Hacker News", ErrorMessage, () => RefreshCommand.ExecuteAsync(null));
    }

    private async Task LoadPageAsync()
    {
        var pageIds = _feedIds.Skip(_loadedCount).Take(PageSize).ToArray();
        _loadedCount += pageIds.Length;

        var items = await _client.GetItemsAsync(pageIds);
        var now = _timeProvider.GetUtcNow();
        foreach (var item in items)
        {
            Stories.Add(new StoryItemViewModel(item, Stories.Count + 1, now));
        }

        HasMore = _loadedCount < _feedIds.Count;
        OnPropertyChanged(nameof(IsEmpty));
    }
}
