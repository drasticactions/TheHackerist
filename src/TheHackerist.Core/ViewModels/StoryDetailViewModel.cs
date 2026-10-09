using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheHackerist.Abstractions;
using TheHackerist.Helpers;
using TheHackerist.Models;
using TheHackerist.Services;

namespace TheHackerist.ViewModels;

public sealed partial class StoryDetailViewModel : ObservableObject
{
    private const int TopLevelPageSize = 15;

    private readonly IHackerNewsClient _client;
    private readonly IBrowserLauncher _browser;
    private readonly IAppSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly IUserNotifier _notifier;
    private readonly INavigator? _navigator;

    private IReadOnlyList<long> _topLevelIds = [];
    private int _loadedTopLevelCount;

    public StoryDetailViewModel(
        IHackerNewsClient client,
        IBrowserLauncher browser,
        IAppSettings settings,
        TimeProvider? timeProvider = null,
        IUserNotifier? notifier = null,
        INavigator? navigator = null)
    {
        _client = client;
        _browser = browser;
        _settings = settings;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _notifier = notifier ?? NullUserNotifier.Instance;
        _navigator = navigator;
    }

    public ObservableCollection<CommentViewModel> Comments { get; } = [];

    [ObservableProperty]
    public partial HnItem? Story { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasMoreComments { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public string Title => Story?.Title ?? string.Empty;

    private string NotificationKey => $"story:{Story?.Id}";

    public string? Domain => Story?.Domain;

    public bool HasUrl => !string.IsNullOrEmpty(Story?.Url);

    public string SubtitleLine => Story is null
        ? string.Empty
        : $"▲ {Story.Score} · {Story.By} · {RelativeTime.Format(Story.TimestampUtc, _timeProvider.GetUtcNow())} · {Story.Descendants} comments";

    public string StoryText => HtmlText.ToPlainText(Story?.Text);

    public bool HasStoryText => !string.IsNullOrEmpty(StoryText);

    public IReadOnlyList<HnTextBlock> StoryBlocks => HnText.Parse(Story?.Text);

    public async Task InitializeAsync(HnItem story)
    {
        Story = story;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Domain));
        OnPropertyChanged(nameof(HasUrl));
        OnPropertyChanged(nameof(SubtitleLine));
        OnPropertyChanged(nameof(StoryText));
        OnPropertyChanged(nameof(HasStoryText));
        OnPropertyChanged(nameof(StoryBlocks));

        Comments.Clear();
        _topLevelIds = story.Kids ?? [];
        _loadedTopLevelCount = 0;

        await LoadCommentsPageAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Story is { } story && !IsLoading)
        {
            await InitializeAsync(story);
        }
    }

    [RelayCommand]
    private Task LoadMoreCommentsAsync() => HasMoreComments ? LoadCommentsPageAsync() : Task.CompletedTask;

    private async Task LoadCommentsPageAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var pageIds = _topLevelIds.Skip(_loadedTopLevelCount).Take(TopLevelPageSize).ToArray();
            _loadedTopLevelCount += pageIds.Length;

            var now = _timeProvider.GetUtcNow();
            var topLevel = await _client.GetItemsAsync(pageIds);
            var roots = await BuildThreadAsync(topLevel);
            foreach (var node in roots)
            {
                Flatten(node, depth: 0, now);
            }

            HasMoreComments = _loadedTopLevelCount < _topLevelIds.Count;
            _notifier.Clear(NotificationKey);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = "Couldn't load comments.";
            _notifier.ShowError(NotificationKey, "Couldn't load comments", Title, () => RefreshCommand.ExecuteAsync(null));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenLinkAsync()
    {
        if (Story?.Url is { Length: > 0 } url)
        {
            await _browser.OpenAsync(url, _settings.BrowserMode);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task OpenUrlAsync(Uri url)
    {
        if (_navigator is not null && HackerNewsLinks.TryGetItemId(url) is { } id)
        {
            try
            {
                if (await _client.GetItemAsync(id) is { IsVisible: true } item)
                {
                    await _navigator.GoToStoryAsync(item);
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
            }
        }

        await _browser.OpenAsync(url.AbsoluteUri, _settings.BrowserMode);
    }

    [RelayCommand]
    private void ToggleCollapse(CommentViewModel? comment)
    {
        if (comment is null || !comment.HasReplies)
        {
            return;
        }

        comment.IsCollapsed = !comment.IsCollapsed;

        var start = Comments.IndexOf(comment);
        if (start < 0)
        {
            return;
        }

        int? hiddenBelowDepth = comment.IsCollapsed ? comment.Depth : null;
        for (var i = start + 1; i < Comments.Count && Comments[i].Depth > comment.Depth; i++)
        {
            var row = Comments[i];
            if (hiddenBelowDepth is { } depth && row.Depth > depth)
            {
                row.IsVisible = false;
                continue;
            }

            row.IsVisible = true;
            hiddenBelowDepth = row.IsCollapsed ? row.Depth : null;
        }
    }

    private async Task<IReadOnlyList<ThreadNode>> BuildThreadAsync(IReadOnlyList<HnItem> roots)
    {
        var rootNodes = roots.Select(r => new ThreadNode(r)).ToArray();
        var level = rootNodes;

        while (level.Length > 0)
        {
            var kidIds = level.SelectMany(n => n.Item.Kids ?? []).ToArray();
            if (kidIds.Length == 0)
            {
                break;
            }

            var fetched = await _client.GetItemsAsync(kidIds);
            var byId = fetched.ToDictionary(i => i.Id);

            var next = new List<ThreadNode>(fetched.Count);
            foreach (var parent in level)
            {
                foreach (var kidId in parent.Item.Kids ?? [])
                {
                    if (byId.TryGetValue(kidId, out var kid))
                    {
                        var child = new ThreadNode(kid);
                        parent.Children.Add(child);
                        next.Add(child);
                    }
                }
            }

            level = [.. next];
        }

        return rootNodes;
    }

    private void Flatten(ThreadNode node, int depth, DateTimeOffset now)
    {
        Comments.Add(new CommentViewModel(node.Item, depth, now, node.DescendantCount));
        foreach (var child in node.Children)
        {
            Flatten(child, depth + 1, now);
        }
    }

    private sealed class ThreadNode(HnItem item)
    {
        public HnItem Item { get; } = item;

        public List<ThreadNode> Children { get; } = [];

        public int DescendantCount => Children.Count + Children.Sum(c => c.DescendantCount);
    }
}
