using TheHackerist.Abstractions;

namespace TheHackerist.Services;

public sealed class DelegatingUserNotifier : IUserNotifier
{
    public IUserNotifier? Target { get; set; }

    public void ShowError(string key, string title, string message, Func<Task>? retry = null) =>
        Target?.ShowError(key, title, message, retry);

    public void Clear(string key) => Target?.Clear(key);
}
