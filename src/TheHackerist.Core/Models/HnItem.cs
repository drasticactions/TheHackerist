using System.Text.Json.Serialization;

namespace TheHackerist.Models;

public sealed record HnItem
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("by")]
    public string? By { get; init; }

    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("descendants")]
    public int Descendants { get; init; }

    [JsonPropertyName("kids")]
    public IReadOnlyList<long>? Kids { get; init; }

    [JsonPropertyName("parent")]
    public long? Parent { get; init; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }

    [JsonPropertyName("dead")]
    public bool Dead { get; init; }

    [JsonIgnore]
    public DateTimeOffset TimestampUtc => DateTimeOffset.FromUnixTimeSeconds(Time);

    [JsonIgnore]
    public bool IsVisible => !Deleted && !Dead;

    [JsonIgnore]
    public string? Domain
    {
        get
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            {
                return null;
            }

            var host = uri.Host;
            return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
        }
    }
}
