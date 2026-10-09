using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace TheHackerist.Android;

[Activity(
    Label = "Hackerist",
    Theme = "@style/HackeristTheme.Splash",
    Icon = "@mipmap/icon",
    RoundIcon = "@mipmap/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);
        base.OnCreate(savedInstanceState);
    }
}
