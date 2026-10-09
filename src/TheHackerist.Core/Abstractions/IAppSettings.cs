namespace TheHackerist.Abstractions;

public interface IAppSettings
{
    BrowserMode BrowserMode { get; set; }

    ThemePreference ThemePreference { get; set; }
}
