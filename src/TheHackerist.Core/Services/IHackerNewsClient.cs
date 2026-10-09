using TheHackerist.Models;

namespace TheHackerist.Services;

public interface IHackerNewsClient
{
    Task<IReadOnlyList<long>> GetFeedIdsAsync(FeedType feed, CancellationToken cancellationToken = default);

    Task<HnItem?> GetItemAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HnItem>> GetItemsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);
}
