using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheHackerist.Abstractions;

namespace TheHackerist.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    public static readonly Uri ProjectUri = new("https://github.com/drasticactions/TheHackerist");

    private readonly IAppSettings _settings;
    private readonly PlatformFeatures _features;
    private readonly IBrowserLauncher? _browser;

    public SettingsViewModel(IAppSettings settings, PlatformFeatures? features = null, IBrowserLauncher? browser = null)
    {
        _settings = settings;
        _features = features ?? new PlatformFeatures();
        _browser = browser;
        _features.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanUseInAppBrowser));
            OnPropertyChanged(nameof(UseInAppBrowser));
        };
    }

    public bool CanUseInAppBrowser => _features.SupportsInAppBrowser;

    public bool UseInAppBrowser
    {
        get => CanUseInAppBrowser && _settings.BrowserMode == BrowserMode.InApp;
        set
        {
            if (CanUseInAppBrowser && UseInAppBrowser != value)
            {
                _settings.BrowserMode = value ? BrowserMode.InApp : BrowserMode.External;
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<string> ThemeOptions { get; } = ["System", "Light", "Dark"];

    public int ThemeIndex
    {
        get => (int)_settings.ThemePreference;
        set
        {
            if (value >= 0 && value != ThemeIndex)
            {
                _settings.ThemePreference = (ThemePreference)value;
                OnPropertyChanged();
            }
        }
    }

    [RelayCommand]
    private Task OpenProjectPageAsync() =>
        _browser?.OpenAsync(ProjectUri.AbsoluteUri, BrowserMode.External) ?? Task.CompletedTask;
}
