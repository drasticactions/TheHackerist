using AvaEnyo;
using Avalonia;
using Avalonia.iOS;
using Foundation;

namespace TheHackerist.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder).WithAvaEnyoFonts();
}
