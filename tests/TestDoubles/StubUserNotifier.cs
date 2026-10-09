using TheHackerist.Abstractions;

namespace TheHackerist.Tests.TestDoubles;

public sealed class StubUserNotifier : IUserNotifier
{
    public Dictionary<string, (string Title, string Message, Func<Task>? Retry)> Shown { get; } = [];

    public int ShowCount { get; private set; }

    public void ShowError(string key, string title, string message, Func<Task>? retry = null)
    {
        ShowCount++;
        Shown[key] = (title, message, retry);
    }

    public void Clear(string key) => Shown.Remove(key);
}
