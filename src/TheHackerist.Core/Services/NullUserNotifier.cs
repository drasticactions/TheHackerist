using TheHackerist.Abstractions;

namespace TheHackerist.Services;

public sealed class NullUserNotifier : IUserNotifier
{
    public static readonly NullUserNotifier Instance = new();

    public void ShowError(string key, string title, string message, Func<Task>? retry = null)
    {
    }

    public void Clear(string key)
    {
    }
}
