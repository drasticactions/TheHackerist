using AvaEnyo.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using TheHackerist.Abstractions;
using TheHackerist.Services;
using TheHackerist.Tests.TestDoubles;
using TheHackerist.Views;

namespace TheHackerist.UI.Tests;

public class PreferencesTests
{
    private static PreferencesView Preferences(ShellFixture f) => TestHost.Find<PreferencesView>(f.View, "PreferencesPage");

    [AvaloniaFact]
    public void AppMenuPreferences_OpensPreferences_AndThemeChoiceApplies()
    {
        var store = new InMemorySettingsStore();
        var f = new ShellFixture(settings: new AppSettings(store, new PlatformFeatures()));

        f.Window.KeyPress(Key.F10, RawInputModifiers.None, PhysicalKey.F10, null);
        f.Settle();
        var item = f.Window.GetVisualDescendants().OfType<AppMenuItem>().FirstOrDefault(i => i.Header as string == "Preferences")
            ?? TestHost.All<AppMenuItem>((Control)TopLevel.GetTopLevel(f.View)!).First(i => i.Header as string == "Preferences");
        TestHost.Click(f.Window, item);

        Assert.True(f.Shell.IsSettingsOpen);
        Assert.True(f.Pump(() => Preferences(f).IsEffectivelyVisible));

        var picker = TestHost.Find<RadioGroup>(f.View, "ThemePicker");
        picker.SelectedIndex = (int)ThemePreference.Dark;
        f.Settle();

        Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
        Assert.Contains("\"Dark\"", store.Json);
        Application.Current.RequestedThemeVariant = ThemeVariant.Default;
    }

    [AvaloniaFact]
    public void Back_ClosesPreferences_AndTheButtonReopensThem()
    {
        var f = new ShellFixture();
        TestHost.Click(f.Window, TestHost.Find<Button>(f.View, "PreferencesButton"));
        Assert.True(f.Pump(() => Preferences(f).IsEffectivelyVisible));

        AvaEnyo.EnyoBack.RequestBack(f.Window);
        f.Settle();
        Assert.False(f.Shell.IsSettingsOpen);

        TestHost.Click(f.Window, TestHost.Find<Button>(f.View, "PreferencesButton"));
        Assert.True(f.Shell.IsSettingsOpen);
        Assert.True(f.Pump(() => Preferences(f).IsEffectivelyVisible));
    }

    [AvaloniaFact]
    public void WithoutInAppBrowser_TheToggleIsHidden()
    {
        var f = new ShellFixture(supportsInAppBrowser: false);
        f.Shell.OpenSettingsCommand.Execute(null);
        f.Settle();

        Assert.DoesNotContain(TestHost.All<ToggleSwitch>(f.View), t => t.Name == "InAppBrowserToggle" && t.IsEffectivelyVisible);
        Assert.True(TestHost.Find<RadioGroup>(f.View, "ThemePicker").IsEffectivelyVisible);
    }

    [AvaloniaTheory]
    [InlineData("LicenseItem", "License", "The MIT License")]
    [InlineData("NoticesItem", "Third-party notices", "Avalonia")]
    public void AboutItem_ShowsItsText_AndBackReturnsToTheList(string item, string title, string expected)
    {
        var f = new ShellFixture();
        f.Shell.OpenSettingsCommand.Execute(null);
        Assert.True(f.Pump(() => Preferences(f).IsEffectivelyVisible));
        f.Settle();

        TestHost.Click(f.Window, TestHost.Find<Item>(f.View, item));
        var notice = TestHost.Find<SelectableTextBlock>(f.View, "NoticeText");
        Assert.True(f.Pump(() => notice.IsEffectivelyVisible));
        Assert.Equal(title, TestHost.Find<PageHeader>(f.View, "NoticeHeader").Content);
        Assert.Contains(expected, notice.Text);
        Assert.DoesNotContain("`", notice.Text);
        Assert.DoesNotContain("**", notice.Text);

        AvaEnyo.EnyoBack.RequestBack(f.Window);
        Assert.True(f.Pump(() => TestHost.Find<RadioGroup>(f.View, "ThemePicker").IsEffectivelyVisible));
        Assert.True(f.Shell.IsSettingsOpen);
    }

    [AvaloniaFact]
    public void ClosingPreferencesFromAText_ReopensOnTheList()
    {
        var f = new ShellFixture();
        f.Shell.OpenSettingsCommand.Execute(null);
        Assert.True(f.Pump(() => Preferences(f).IsEffectivelyVisible));
        f.Settle();
        TestHost.Click(f.Window, TestHost.Find<Item>(f.View, "LicenseItem"));
        Assert.True(f.Pump(() => TestHost.Find<SelectableTextBlock>(f.View, "NoticeText").IsEffectivelyVisible));

        f.Shell.IsSettingsOpen = false;
        Assert.True(f.Pump(() => !Preferences(f).IsEffectivelyVisible));
        f.Shell.OpenSettingsCommand.Execute(null);

        Assert.True(f.Pump(() => TestHost.Find<RadioGroup>(f.View, "ThemePicker").IsEffectivelyVisible));
    }

    [AvaloniaFact]
    public void SourceCodeItem_OpensGitHubExternally()
    {
        var f = new ShellFixture();
        f.Shell.OpenSettingsCommand.Execute(null);
        Assert.True(f.Pump(() => Preferences(f).IsEffectivelyVisible));

        f.Settle();
        TestHost.Click(f.Window, TestHost.Find<Item>(f.View, "SourceCodeItem"));

        Assert.Contains(("https://github.com/drasticactions/TheHackerist", BrowserMode.External), f.Browser.Opened);
    }
}
