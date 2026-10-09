using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using TheHackerist.Abstractions;
using TheHackerist.Serialization;

namespace TheHackerist.Services;

public sealed class AppSettings : IAppSettings
{
    private readonly ISettingsStore _store;
    private readonly PlatformFeatures _features;
    private SettingsData _data;

    public AppSettings(ISettingsStore store, PlatformFeatures features)
    {
        _store = store;
        _features = features;
        _data = Load(store);
    }

    public BrowserMode BrowserMode
    {
        get => _features.SupportsInAppBrowser ? _data.BrowserMode : BrowserMode.External;
        set => Save(_data with { BrowserMode = value });
    }

    public ThemePreference ThemePreference
    {
        get => _data.ThemePreference;
        set
        {
            Save(_data with { ThemePreference = value });
            ApplyTheme();
        }
    }

    public void ApplyTheme()
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = _data.ThemePreference switch
            {
                ThemePreference.Light => ThemeVariant.Light,
                ThemePreference.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }

    private static SettingsData Load(ISettingsStore store)
    {
        if (store.Read() is not { Length: > 0 } json)
        {
            return new SettingsData();
        }

        try
        {
            return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SettingsData) ?? new SettingsData();
        }
        catch (JsonException ex)
        {
            Trace.WriteLine($"Ignoring unreadable settings: {ex.Message}");
            return new SettingsData();
        }
    }

    private void Save(SettingsData data)
    {
        if (data == _data)
        {
            return;
        }

        _data = data;
        _store.Write(JsonSerializer.Serialize(data, SettingsJsonContext.Default.SettingsData));
    }
}
