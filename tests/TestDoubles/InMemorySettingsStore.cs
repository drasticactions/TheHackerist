using TheHackerist.Abstractions;

namespace TheHackerist.Tests.TestDoubles;

public sealed class InMemorySettingsStore(string? json = null) : ISettingsStore
{
    public string? Json { get; private set; } = json;

    public int WriteCount { get; private set; }

    public string? Read() => Json;

    public void Write(string json)
    {
        WriteCount++;
        Json = json;
    }
}
