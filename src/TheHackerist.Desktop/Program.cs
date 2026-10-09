using System.Globalization;
using AvaEnyo;
using AvaEnyo.Animations;
using Avalonia;
using Avalonia.Styling;

namespace TheHackerist.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var options = LaunchOptions.Parse(args);
        if (options.TimeScale is { } scale)
        {
            EnyoAnimations.TimeScale = scale;
        }

        BuildAvaloniaApp()
            .AfterSetup(builder =>
            {
                var app = (App)builder.Instance!;
                app.ThemeOverride = options.Variant;
                app.StartupUrl = options.OpenUrl;
            })
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .WithAvaEnyoFonts()
            .UseWaylandWithFallback()
#pragma warning disable AVALONIA_WAYLAND_FORCE_CSD
            .With(new WaylandPlatformOptions { ForceDrawnDecorations = true })
#pragma warning restore AVALONIA_WAYLAND_FORCE_CSD
            .LogToTrace();

    private sealed record LaunchOptions(ThemeVariant? Variant, Uri? OpenUrl, double? TimeScale)
    {
        public static LaunchOptions Parse(string[] args)
        {
            ThemeVariant? variant = null;
            Uri? openUrl = null;
            double? timeScale = null;

            for (var i = 0; i < args.Length - 1; i++)
            {
                var value = args[i + 1];
                switch (args[i])
                {
                    case "--variant":
                        variant = value.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? ThemeVariant.Dark : ThemeVariant.Light;
                        i++;
                        break;
                    case "--open-url" when Uri.TryCreate(value, UriKind.Absolute, out var uri):
                        openUrl = uri;
                        i++;
                        break;
                    case "--time-scale" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var s):
                        timeScale = s;
                        i++;
                        break;
                }
            }

            return new LaunchOptions(variant, openUrl, timeScale);
        }
    }
}
