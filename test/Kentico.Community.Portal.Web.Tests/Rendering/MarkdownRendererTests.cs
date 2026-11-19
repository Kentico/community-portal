using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Tests.Rendering;

public class MarkdownRendererTests
{
    [Test]
    public void RenderInlineUnsafe_Omits_Paragraph_Tags()
    {
        var sut = new MarkdownRenderer();

        string html = sut.RenderInlineUnsafe("Read the [Terms](/terms).").Value!;

        Assert.That(html, Is.EqualTo("""Read the <a href="/terms">Terms</a>."""));
    }

    [Test]
    public void RenderInlineUnsafe_Supports_Link_Attributes()
    {
        var sut = new MarkdownRenderer();

        string html = sut.RenderInlineUnsafe("""[Terms](/terms){target="_blank"}""").Value!;

        Assert.That(html, Is.EqualTo("""<a href="/terms" target="_blank">Terms</a>"""));
    }

    [Test]
    public void RenderUnsafe_Wraps_Paragraphs()
    {
        var sut = new MarkdownRenderer();

        string html = sut.RenderUnsafe("Text").Value!;

        Assert.That(html.Trim(), Is.EqualTo("<p>Text</p>"));
    }

    [Test]
    public void ToPlainTextExcerpt_Strips_Markdown_To_One_Line()
    {
        var sut = new MarkdownRenderer();

        string text = sut.ToPlainTextExcerpt("## Heading\n\nSome **bold** [link](/x).\n\n```\ncode\n```");

        Assert.That(text, Is.EqualTo("Heading Some bold link. code"));
    }

    [Test]
    public void ToPlainTextExcerpt_Truncates_At_Word_Boundary()
    {
        var sut = new MarkdownRenderer();

        string text = sut.ToPlainTextExcerpt("alpha beta gamma delta", 12);

        Assert.That(text, Is.EqualTo("alpha beta…"));
    }
}
