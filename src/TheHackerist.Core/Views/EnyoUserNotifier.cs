using AvaEnyo.Services;
using Avalonia.Threading;
using TheHackerist.Abstractions;

namespace TheHackerist.Views;

public sealed class EnyoUserNotifier : IUserNotifier, IDisposable
{
    private readonly INotificationService _service;
    private readonly HashSet<string> _showing = [];

    public EnyoUserNotifier(INotificationService service)
    {
        _service = service;
        _service.Activated += OnActivated;
        _service.Dismissed += OnDismissed;
    }

    public void ShowError(string key, string title, string message, Func<Task>? retry = null) =>
        Dispatcher.UIThread.Post(() =>
        {
            var notification = new Notification(key)
            {
                Title = title,
                Text = retry is null ? message : $"{message} Tap to retry.",
                Tag = retry,
            };

            _showing.Add(key);
            _service.Show(notification);
        });

    public void Clear(string key) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (_showing.Remove(key))
            {
                _service.Dismiss(key);
            }
        });

    public void Dispose()
    {
        _service.Activated -= OnActivated;
        _service.Dismissed -= OnDismissed;
    }

    private void OnActivated(object? sender, NotificationEventArgs e)
    {
        var key = e.Notification.Id;
        if (_showing.Remove(key))
        {
            _service.Dismiss(key);
        }

        if (e.Notification.Tag is Func<Task> retry)
        {
            _ = retry();
        }
    }

    private void OnDismissed(object? sender, NotificationEventArgs e) => _showing.Remove(e.Notification.Id);
}
