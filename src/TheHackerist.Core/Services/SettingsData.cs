using TheHackerist.Abstractions;

namespace TheHackerist.Services;

public sealed record SettingsData
{
    public BrowserMode BrowserMode { get; init; } = BrowserMode.InApp;

    public ThemePreference ThemePreference { get; init; } = ThemePreference.System;
}
