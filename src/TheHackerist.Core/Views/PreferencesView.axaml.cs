using Avalonia.Controls;
using Avalonia.Platform;
using TheHackerist.ViewModels;

namespace TheHackerist.Views;

public partial class PreferencesView : UserControl
{
    public PreferencesView()
    {
        InitializeComponent();

        SourceCodeItem.Click += (_, _) => (DataContext as SettingsViewModel)?.OpenProjectPageCommand.Execute(null);
        LicenseItem.Click += (_, _) => ShowNotice("License", "LICENSE.md");
        NoticesItem.Click += (_, _) => ShowNotice("Third-party notices", "THIRD-PARTY-NOTICES.md");
    }

    public void ShowSettings()
    {
        if (PreferencesPane.SelectedView != SettingsPage)
        {
            PreferencesPane.Back();
        }
    }

    private void ShowNotice(string title, string asset)
    {
        NoticeHeader.Content = title;
        NoticeText.Text = ReadNotice(asset);
        PreferencesPane.SelectView(NoticePage);
    }

    private static string ReadNotice(string asset)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://TheHackerist.Core/Assets/{asset}"));
        using var reader = new StreamReader(stream);
        var lines = reader.ReadToEnd().ReplaceLineEndings("\n").Split('\n')
            .Where(l => l.TrimEnd() != "```")
            .Select(l => (l.StartsWith('#') ? l.TrimStart('#').TrimStart() : l).Replace("**", "").Replace("`", ""));
        return string.Join('\n', lines).Trim();
    }
}
