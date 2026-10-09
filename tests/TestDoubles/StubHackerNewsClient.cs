using TheHackerist.Models;
using TheHackerist.Services;

namespace TheHackerist.Tests.TestDoubles;

public sealed class StubHackerNewsClient : IHackerNewsClient
{
    public Dictionary<FeedType, IReadOnlyList<long>> Feeds { get; } = [];

    public Dictionary<long, HnItem> Items { get; } = [];

    public int GetItemsCallCount { get; private set; }

    public int GetFeedIdsCallCount { get; private set; }

    public Exception? Failure { get; set; }

    public void AddItem(HnItem item) => Items[item.Id] = item;

    public Task<IReadOnlyList<long>> GetFeedIdsAsync(FeedType feed, CancellationToken cancellationToken = default)
    {
        GetFeedIdsCallCount++;
        if (Failure is not null)
        {
            return Task.FromException<IReadOnlyList<long>>(Failure);
        }

        return Task.FromResult(Feeds.GetValueOrDefault(feed) ?? (IReadOnlyList<long>)Array.Empty<long>());
    }

    public Task<HnItem?> GetItemAsync(long id, CancellationToken cancellationToken = default) =>
        Failure is not null ? Task.FromException<HnItem?>(Failure) : Task.FromResult(Items.GetValueOrDefault(id));

    public Task<IReadOnlyList<HnItem>> GetItemsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        GetItemsCallCount++;
        if (Failure is not null)
        {
            return Task.FromException<IReadOnlyList<HnItem>>(Failure);
        }

        IReadOnlyList<HnItem> result = [.. ids
            .Select(id => Items.GetValueOrDefault(id))
            .Where(i => i is { IsVisible: true })
            .Cast<HnItem>()];
        return Task.FromResult(result);
    }
}
