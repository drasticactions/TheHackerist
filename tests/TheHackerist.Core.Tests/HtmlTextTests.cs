using TheHackerist.Helpers;

namespace TheHackerist.Tests;

public class HtmlTextTests
{
    [Fact]
    public void NullOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, HtmlText.ToPlainText(null));
        Assert.Equal(string.Empty, HtmlText.ToPlainText("   "));
    }

    [Fact]
    public void Paragraphs_BecomeBlankLines()
    {
        var result = HtmlText.ToPlainText("first<p>second");
        Assert.Equal("first\n\nsecond", result);
    }

    [Fact]
    public void Entities_AreDecoded()
    {
        var result = HtmlText.ToPlainText("a &gt; b &amp;&amp; c &#x27;quoted&#x27;");
        Assert.Equal("a > b && c 'quoted'", result);
    }

    [Fact]
    public void AnchorTags_KeepVisibleText()
    {
        var result = HtmlText.ToPlainText("see <a href=\"https://example.com\" rel=\"nofollow\">https:&#x2F;&#x2F;example.com</a>");
        Assert.Equal("see https://example.com", result);
    }

    [Fact]
    public void LineBreaks_BecomeNewlines()
    {
        Assert.Equal("one\ntwo", HtmlText.ToPlainText("one<br>two"));
        Assert.Equal("one\ntwo", HtmlText.ToPlainText("one<br/>two"));
    }

    [Fact]
    public void ItalicAndCodeTags_AreStripped()
    {
        var result = HtmlText.ToPlainText("<i>emphasis</i> and <pre><code>var x = 1;</code></pre>");
        Assert.Equal("emphasis and var x = 1;", result);
    }
}
