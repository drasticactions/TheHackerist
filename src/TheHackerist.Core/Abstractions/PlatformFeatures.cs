namespace TheHackerist.Abstractions;

public sealed class PlatformFeatures(bool supportsInAppBrowser = true)
{
    public bool SupportsInAppBrowser { get; private set; } = supportsInAppBrowser;

    public event EventHandler? Changed;

    public void DisableInAppBrowser()
    {
        if (SupportsInAppBrowser)
        {
            SupportsInAppBrowser = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
