using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using TheHackerist.Models;
using TheHackerist.Serialization;

namespace TheHackerist.Services;

public sealed class HackerNewsClient : IHackerNewsClient
{
    public static readonly Uri BaseAddress = new("https://hacker-news.firebaseio.com/v0/");

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const int MaxConcurrentItemRequests = 8;

    private readonly HttpClient _http;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<long, CacheEntry> _itemCache = new();
    private readonly SemaphoreSlim _itemGate = new(MaxConcurrentItemRequests);

    public HackerNewsClient(HttpClient http, TimeProvider? timeProvider = null)
    {
        _http = http;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<long>> GetFeedIdsAsync(FeedType feed, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(BaseAddress, $"{feed.ToEndpoint()}.json");
        var ids = await _http.GetFromJsonAsync(uri, HnJsonContext.Default.Int64Array, cancellationToken)
            .ConfigureAwait(false);
        return ids ?? [];
    }

    public async Task<HnItem?> GetItemAsync(long id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (_itemCache.TryGetValue(id, out var cached) && now - cached.FetchedAt < CacheTtl)
        {
            return cached.Item;
        }

        await _itemGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var uri = new Uri(BaseAddress, $"item/{id}.json");
            HnItem? item;
            try
            {
                item = await _http.GetFromJsonAsync(uri, HnJsonContext.Default.HnItem, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                item = null;
            }

            if (item is not null)
            {
                _itemCache[id] = new CacheEntry(item, _timeProvider.GetUtcNow());
            }

            return item;
        }
        finally
        {
            _itemGate.Release();
        }
    }

    public async Task<IReadOnlyList<HnItem>> GetItemsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var tasks = ids.Select(id => GetItemAsync(id, cancellationToken)).ToArray();
        var items = await Task.WhenAll(tasks).ConfigureAwait(false);
        return [.. items.Where(i => i is { IsVisible: true })!];
    }

    private readonly record struct CacheEntry(HnItem Item, DateTimeOffset FetchedAt);
}
