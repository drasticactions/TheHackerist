using AvaEnyo;
using AvaEnyo.Animations;
using AvaEnyo.Controls;
using AvaEnyo.Services;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;

[assembly: AvaloniaTestApplication(typeof(TheHackerist.UI.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace TheHackerist.UI.Tests;

public class TestApplication : Application
{
    public TestApplication()
    {
        EnyoAnimations.TimeScale = 0;
        Styles.Add(new AvaEnyoTheme());
        Styles.Add(new AvaEnyoControlsTheme());
        Styles.Add(new AvaEnyoServicesTheme());
        Styles.Add(new StyleInclude(new Uri("avares://TheHackerist.UI.Tests"))
        {
            Source = new Uri("avares://TheHackerist.Core/Views/HackeristStyles.axaml"),
        });
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .WithAvaEnyoFonts()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
