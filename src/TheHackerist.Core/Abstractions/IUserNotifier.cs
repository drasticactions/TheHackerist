namespace TheHackerist.Abstractions;

public interface IUserNotifier
{
    void ShowError(string key, string title, string message, Func<Task>? retry = null);

    void Clear(string key);
}
