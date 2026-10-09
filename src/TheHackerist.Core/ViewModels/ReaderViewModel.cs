using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheHackerist.Abstractions;
using TheHackerist.Services;

namespace TheHackerist.ViewModels;

public enum ReaderAction
{
    Back,
    Forward,
    Reload,
}

public sealed partial class ReaderViewModel : ObservableObject
{
    private readonly IBrowserLauncher? _browser;
    private readonly PlatformFeatures? _features;
    private readonly IUserNotifier _notifier;
    private readonly Action? _close;

    public ReaderViewModel(
        Uri url,
        IBrowserLauncher? browser = null,
        PlatformFeatures? features = null,
        IUserNotifier? notifier = null,
        Action? close = null)
    {
        Url = url;
        Host = url.Host;
        CurrentUrl = url;
        _browser = browser;
        _features = features;
        _notifier = notifier ?? NullUserNotifier.Instance;
        _close = close;
    }

    public Uri Url { get; }

    public string Host { get; }

    [ObservableProperty]
    public partial Uri CurrentUrl { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    public partial bool CanGoBack { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoForwardCommand))]
    public partial bool CanGoForward { get; set; }

    public event EventHandler<ReaderAction>? ActionRequested;

    public async Task ReportWebViewUnavailableAsync()
    {
        _features?.DisableInAppBrowser();
        _notifier.ShowError("reader:unavailable", "In-app browser unavailable", "Links open in your browser.");
        _close?.Invoke();
        if (_browser is not null)
        {
            await _browser.OpenAsync(Url.AbsoluteUri, BrowserMode.External);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => ActionRequested?.Invoke(this, ReaderAction.Back);

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForward() => ActionRequested?.Invoke(this, ReaderAction.Forward);

    [RelayCommand]
    private void Reload() => ActionRequested?.Invoke(this, ReaderAction.Reload);

    [RelayCommand]
    private Task OpenExternallyAsync() =>
        _browser?.OpenAsync(CurrentUrl.AbsoluteUri, BrowserMode.External) ?? Task.CompletedTask;
}
