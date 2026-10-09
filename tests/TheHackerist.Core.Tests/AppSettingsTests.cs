using TheHackerist.Abstractions;
using TheHackerist.Services;
using TheHackerist.Tests.TestDoubles;

namespace TheHackerist.Tests;

public class AppSettingsTests
{
    [Fact]
    public void EmptyStore_GivesDefaults()
    {
        var settings = new AppSettings(new InMemorySettingsStore(), new PlatformFeatures());

        Assert.Equal(BrowserMode.InApp, settings.BrowserMode);
        Assert.Equal(ThemePreference.System, settings.ThemePreference);
    }

    [Fact]
    public void Changes_AreSavedAndReadBack()
    {
        var store = new InMemorySettingsStore();
        var settings = new AppSettings(store, new PlatformFeatures());

        settings.BrowserMode = BrowserMode.External;
        settings.ThemePreference = ThemePreference.Dark;

        Assert.Contains("\"External\"", store.Json);
        Assert.Contains("\"Dark\"", store.Json);

        var reloaded = new AppSettings(store, new PlatformFeatures());
        Assert.Equal(BrowserMode.External, reloaded.BrowserMode);
        Assert.Equal(ThemePreference.Dark, reloaded.ThemePreference);
    }

    [Fact]
    public void SettingTheSameValue_DoesNotWrite()
    {
        var store = new InMemorySettingsStore();
        var settings = new AppSettings(store, new PlatformFeatures());

        settings.BrowserMode = BrowserMode.InApp;

        Assert.Equal(0, store.WriteCount);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{\"BrowserMode\":\"Sideways\"}")]
    [InlineData("[]")]
    public void UnreadableDocument_GivesDefaultsAndIsReplacedOnSave(string json)
    {
        var store = new InMemorySettingsStore(json);
        var settings = new AppSettings(store, new PlatformFeatures());

        Assert.Equal(BrowserMode.InApp, settings.BrowserMode);

        settings.ThemePreference = ThemePreference.Light;
        Assert.Equal(ThemePreference.Light, new AppSettings(store, new PlatformFeatures()).ThemePreference);
    }

    [Fact]
    public void WithoutInAppBrowser_ModeReadsExternalButStoredValueIsKept()
    {
        var store = new InMemorySettingsStore();
        var features = new PlatformFeatures();
        var settings = new AppSettings(store, features);

        features.DisableInAppBrowser();

        Assert.Equal(BrowserMode.External, settings.BrowserMode);
        Assert.Equal(0, store.WriteCount);
        Assert.Equal(BrowserMode.InApp, new AppSettings(store, new PlatformFeatures()).BrowserMode);
    }

    [Fact]
    public void FileStore_RoundTripsThroughANewDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hackerist-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileSettingsStore(Path.Combine(dir, "nested", "settings.json"));
            Assert.Null(store.Read());

            store.Write("{\"a\":1}");
            store.Write("{\"a\":2}");

            Assert.Equal("{\"a\":2}", store.Read());
            Assert.False(File.Exists(store.Path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
