using System.Text.Json;
using TheHackerist.Helpers;
using TheHackerist.Models;
using TheHackerist.Serialization;

namespace TheHackerist.Tests;

public class HnTextTests
{
    private static HnParagraph Paragraph(HnTextBlock block) => Assert.IsType<HnParagraph>(block);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("<p></p>")]
    public void Empty_GivesNoBlocks(string? html) => Assert.Empty(HnText.Parse(html));

    [Fact]
    public void Paragraphs_SplitOnP()
    {
        var blocks = HnText.Parse("First line.<p>Second &amp; last.");

        Assert.Equal(2, blocks.Count);
        Assert.Equal([new HnRun("First line.")], Paragraph(blocks[0]).Inlines);
        Assert.Equal([new HnRun("Second & last.")], Paragraph(blocks[1]).Inlines);
    }

    [Fact]
    public void Italics_BecomeItalicRuns()
    {
        var p = Paragraph(Assert.Single(HnText.Parse("This is <i>really</i> it")));

        Assert.Equal([new HnRun("This is "), new HnRun("really", Italic: true), new HnRun(" it")], p.Inlines);
    }

    [Fact]
    public void Links_KeepTheFullHrefWhenTheTextIsShortened()
    {
        var html = "See <a href=\"https:&#x2F;&#x2F;example.com&#x2F;a&#x2F;very&#x2F;long&#x2F;path?x=1&amp;y=2\" rel=\"nofollow\">https:&#x2F;&#x2F;example.com&#x2F;a&#x2F;very&#x2F;...</a> now";

        var p = Paragraph(Assert.Single(HnText.Parse(html)));

        Assert.Equal(3, p.Inlines.Count);
        var link = Assert.IsType<HnLink>(p.Inlines[1]);
        Assert.Equal("https://example.com/a/very/...", link.Text);
        Assert.Equal(new Uri("https://example.com/a/very/long/path?x=1&y=2"), link.Url);
    }

    [Theory]
    [InlineData("<a href=\"item?id=1\">relative</a>")]
    [InlineData("<a href=\"javascript:alert(1)\">script</a>")]
    [InlineData("<a>no href</a>")]
    public void LinksThatAreNotAbsoluteHttp_BecomeText(string html)
    {
        var p = Paragraph(Assert.Single(HnText.Parse(html)));

        Assert.IsType<HnRun>(Assert.Single(p.Inlines));
    }

    [Fact]
    public void PreCode_BecomesACodeBlockWithItsLayoutKept()
    {
        var html = "Try this:<p><pre><code>  if (a &lt; b)\n    return;\n</code></pre>Done.";

        var blocks = HnText.Parse(html);

        Assert.Equal(3, blocks.Count);
        Assert.Equal("  if (a < b)\n    return;", Assert.IsType<HnCodeBlock>(blocks[1]).Code);
        Assert.Equal([new HnRun("Done.")], Paragraph(blocks[2]).Inlines);
    }

    [Fact]
    public void UnknownTags_AreDroppedAndTheirTextKept()
    {
        var p = Paragraph(Assert.Single(HnText.Parse("<b>bold</b> and <span class=\"x\">span</span>")));

        Assert.Equal([new HnRun("bold and span")], p.Inlines);
    }

    [Fact]
    public void UnclosedTags_AreClosedAtTheEnd()
    {
        var blocks = HnText.Parse("<i>open italic <a href=\"https://example.com\">open link");

        var p = Paragraph(Assert.Single(blocks));
        Assert.Equal(new HnRun("open italic ", Italic: true), p.Inlines[0]);
        Assert.Equal(new HnLink("open link", new Uri("https://example.com")), p.Inlines[1]);
    }

    [Fact]
    public void StrayLessThan_IsText()
    {
        var p = Paragraph(Assert.Single(HnText.Parse("a < b")));

        Assert.Equal([new HnRun("a < b")], p.Inlines);
    }

    [Fact]
    public void LineBreaks_StayInsideTheParagraph()
    {
        var p = Paragraph(Assert.Single(HnText.Parse("one<br>two")));

        Assert.Equal([new HnRun("one\ntwo")], p.Inlines);
    }

    private static string FixtureText(long id)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"item-{id}.json"));
        return JsonSerializer.Deserialize(json, HnJsonContext.Default.HnItem)!.Text!;
    }

    [Fact]
    public void RealComment_WithCodeAndAShortenedLink()
    {
        var blocks = HnText.Parse(FixtureText(9972887));

        var code = Assert.Single(blocks.OfType<HnCodeBlock>());
        Assert.StartsWith("    mov sp, ret1\n    jmp block", code.Code);
        Assert.EndsWith("    jmp sp", code.Code);

        var link = Assert.Single(blocks.OfType<HnParagraph>().SelectMany(p => p.Inlines).OfType<HnLink>());
        Assert.EndsWith("...", link.Text);
        Assert.Equal("Call_Instruction_Peculiarity", link.Url.Fragment.TrimStart('#'));

        var afterCode = Paragraph(blocks[blocks.ToList().IndexOf(code) + 1]);
        Assert.StartsWith("You can even", afterCode.Inlines[0].Text);
        Assert.DoesNotContain(blocks.OfType<HnParagraph>().SelectMany(p => p.Inlines), i => i.Text.Contains("&#x27;"));
    }

    [Fact]
    public void RealComment_WithItalicsAndALink()
    {
        var blocks = HnText.Parse(FixtureText(99660));

        Assert.Equal(4, blocks.Count);
        var first = Paragraph(blocks[0]);
        Assert.True(Assert.IsType<HnRun>(Assert.Single(first.Inlines)).Italic);

        var link = Assert.Single(Paragraph(blocks[1]).Inlines.OfType<HnLink>());
        Assert.Equal(new Uri("http://blog.pmarca.com/2007/09/the-three-kinds.html"), link.Url);
    }
}
