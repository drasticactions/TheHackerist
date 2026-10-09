using TheHackerist.Abstractions;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.ViewModels;

namespace TheHackerist.Tests;

public class SettingsViewModelTests
{
    private sealed class FixedSettings : IAppSettings
    {
        public BrowserMode BrowserMode { get; set; }

        public ThemePreference ThemePreference { get; set; }
    }

    [Fact]
    public void UseInAppBrowser_RoundTripsToBrowserMode()
    {
        var settings = new FixedSettings { BrowserMode = BrowserMode.InApp };
        var vm = new SettingsViewModel(settings);

        Assert.True(vm.UseInAppBrowser);

        vm.UseInAppBrowser = false;
        Assert.Equal(BrowserMode.External, settings.BrowserMode);

        vm.UseInAppBrowser = true;
        Assert.Equal(BrowserMode.InApp, settings.BrowserMode);
    }

    [Fact]
    public void ThemeIndex_RoundTripsToThemePreference()
    {
        var settings = new FixedSettings { ThemePreference = ThemePreference.System };
        var vm = new SettingsViewModel(settings);

        Assert.Equal(0, vm.ThemeIndex);
        Assert.Equal(["System", "Light", "Dark"], vm.ThemeOptions);

        vm.ThemeIndex = 2;
        Assert.Equal(ThemePreference.Dark, settings.ThemePreference);

        vm.ThemeIndex = 1;
        Assert.Equal(ThemePreference.Light, settings.ThemePreference);
    }

    [Fact]
    public void ThemeIndex_IgnoresNegativeIndex()
    {
        var settings = new FixedSettings { ThemePreference = ThemePreference.Dark };
        var vm = new SettingsViewModel(settings);

        vm.ThemeIndex = -1;

        Assert.Equal(ThemePreference.Dark, settings.ThemePreference);
    }

    [Fact]
    public void WithoutInAppBrowser_ToggleIsHiddenAndReadsOff()
    {
        var settings = new FixedSettings { BrowserMode = BrowserMode.InApp };
        var vm = new SettingsViewModel(settings, new PlatformFeatures(supportsInAppBrowser: false));

        Assert.False(vm.CanUseInAppBrowser);
        Assert.False(vm.UseInAppBrowser);

        vm.UseInAppBrowser = true;
        Assert.Equal(BrowserMode.InApp, settings.BrowserMode);
    }

    [Fact]
    public void DisablingInAppBrowser_RaisesTheToggleProperties()
    {
        var features = new PlatformFeatures();
        var vm = new SettingsViewModel(new FixedSettings { BrowserMode = BrowserMode.InApp }, features);
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        features.DisableInAppBrowser();

        Assert.Contains(nameof(SettingsViewModel.CanUseInAppBrowser), changed);
        Assert.Contains(nameof(SettingsViewModel.UseInAppBrowser), changed);
        Assert.False(vm.UseInAppBrowser);
    }

    [Fact]
    public async Task OpenProjectPage_OpensGitHubInTheSystemBrowser()
    {
        var browser = new StubBrowserLauncher();
        var vm = new SettingsViewModel(new FixedSettings { BrowserMode = BrowserMode.InApp }, browser: browser);

        await vm.OpenProjectPageCommand.ExecuteAsync(null);

        Assert.Equal([("https://github.com/drasticactions/TheHackerist", BrowserMode.External)], browser.Opened);
    }
}
