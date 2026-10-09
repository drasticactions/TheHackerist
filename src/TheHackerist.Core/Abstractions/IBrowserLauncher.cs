namespace TheHackerist.Abstractions;

public enum BrowserMode
{
    InApp,

    External,
}

public interface IBrowserLauncher
{
    Task OpenAsync(string url, BrowserMode mode);
}
