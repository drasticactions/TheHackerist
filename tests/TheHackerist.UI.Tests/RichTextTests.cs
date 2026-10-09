using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CommunityToolkit.Mvvm.Input;
using TheHackerist.Helpers;
using TheHackerist.Views;

namespace TheHackerist.UI.Tests;

public class RichTextTests
{
    private static (Window Window, SelectableTextBlock Text, List<Uri> Opened) Show(string html)
    {
        var opened = new List<Uri>();
        var text = new SelectableTextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Width = 400 };
        RichText.SetLinkCommand(text, new RelayCommand<Uri>(u => opened.Add(u!)));
        RichText.SetBlocks(text, HnText.Parse(html));
        var window = TestHost.Show(new StackPanel { Children = { text } }, 600, 400);
        return (window, text, opened);
    }

    [AvaloniaFact]
    public void TappingALink_RunsTheCommandWithItsUrl()
    {
        var (window, text, opened) = Show("Before <a href=\"https://example.com/x\">the link</a> after");

        var linkStart = text.TextLayout.HitTestTextPosition("Before ".Length + 2);
        TestHost.Click(window, text, new Point(linkStart.X + 1, linkStart.Y + linkStart.Height / 2));

        Assert.Equal([new Uri("https://example.com/x")], opened);
    }

    [AvaloniaFact]
    public void TappingPlainText_RunsNothing()
    {
        var (window, text, opened) = Show("Before <a href=\"https://example.com/x\">the link</a> after");

        TestHost.Click(window, text, new Point(2, 5));

        Assert.Empty(opened);
    }

    [AvaloniaFact]
    public void TappingALink_AfterParagraphsAndItalics()
    {
        var (window, text, opened) = Show("Related:<p><i>An italic title</i> - <a href=\"https://example.com/x\">the link</a> after<p>more");

        var offset = "Related:\n\nAn italic title - ".Length + 2;
        var linkStart = text.TextLayout.HitTestTextPosition(offset);
        TestHost.Click(window, text, new Point(linkStart.X + 1, linkStart.Y + linkStart.Height / 2));

        Assert.Equal([new Uri("https://example.com/x")], opened);
    }

    [AvaloniaFact]
    public void TappingTheSecondLineOfAWrappedLink_OpensIt()
    {
        var (window, text, opened) = Show("Words before it <a href=\"https://example.com/long\">a link whose text is long enough that it has to wrap onto another line here</a> end");

        var end = "Words before it a link whose text is long enough that it has to wrap onto another line here".Length - 2;
        var lastChar = text.TextLayout.HitTestTextPosition(end);
        var firstChar = text.TextLayout.HitTestTextPosition("Words before it ".Length);
        Assert.True(lastChar.Y > firstChar.Y, "The link should wrap for this test.");
        TestHost.Click(window, text, new Point(lastChar.X - 1, lastChar.Y + lastChar.Height / 2));

        Assert.Equal([new Uri("https://example.com/long")], opened);
    }
}
