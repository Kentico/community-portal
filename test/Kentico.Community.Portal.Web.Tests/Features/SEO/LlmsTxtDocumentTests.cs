using Kentico.Community.Portal.Web.Features.SEO;

namespace Kentico.Community.Portal.Web.Tests.Features.SEO;

public class LlmsTxtDocumentTests
{
    private const string BASE_URL = "https://example.test/";

    [Test]
    public void Renders_Title_Summary_And_Sections()
    {
        var document = new LlmsTxtDocument([
            new("Key pages", [new("Q&A", "/q-and-a", "Questions and answers")]),
            new("Extensions and Integrations", [new("Lucene", "https://github.com/Kentico/xperience-by-kentico-lucene", "")]),
        ]);

        Assert.That(document.ToMarkdown("Kentico Community", "Developer resources", BASE_URL), Is.EqualTo(
            "# Kentico Community\n" +
            "\n" +
            "> Developer resources\n" +
            "\n" +
            "## Key pages\n" +
            "\n" +
            "- [Q&A](https://example.test/q-and-a): Questions and answers\n" +
            "\n" +
            "## Extensions and Integrations\n" +
            "\n" +
            "- [Lucene](https://github.com/Kentico/xperience-by-kentico-lucene)\n"));
    }

    [Test]
    public void Omits_Empty_Sections()
    {
        var document = new LlmsTxtDocument([new("Topic hubs", [])]);

        Assert.That(document.ToMarkdown("Site", "Summary", BASE_URL), Does.Not.Contain("## Topic hubs"));
    }

    [Test]
    public void Keeps_Links_On_One_Line_And_Escapes_Markdown()
    {
        var document = new LlmsTxtDocument([
            new("Blog", [new("Tips [part 1]", "/blog/a post (draft)", "Line one\r\n  line two")]),
        ]);

        Assert.That(document.ToMarkdown("Site", "Summary", BASE_URL), Does.Contain(
            "- [Tips \\[part 1\\]](https://example.test/blog/a%20post%20%28draft%29): Line one line two\n"));
    }
}
