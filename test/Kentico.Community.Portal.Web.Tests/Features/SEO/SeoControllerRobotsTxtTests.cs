using Kentico.Community.Portal.Web.Features.SEO;

namespace Kentico.Community.Portal.Web.Tests.Features.SEO;

public class SeoControllerRobotsTxtTests
{
    private const string SITEMAP_URL = "https://example.test/sitemap.xml";

    [Test]
    public void Appends_Sitemap_After_Editorial_Body() =>
        Assert.That(
            SeoController.AppendSitemapDirective("User-agent: *\r\nAllow: /\r\n", SITEMAP_URL),
            Is.EqualTo($"User-agent: *\r\nAllow: /\n\nSitemap: {SITEMAP_URL}\n"));

    [Test]
    public void Empty_Body_Is_Sitemap_Only() =>
        Assert.That(
            SeoController.AppendSitemapDirective("", SITEMAP_URL),
            Is.EqualTo($"Sitemap: {SITEMAP_URL}\n"));

    [TestCase("User-agent: *\nAllow: /\nSitemap: https://other.test/sitemap.xml\n")]
    [TestCase("User-agent: *\r\nAllow: /\r\n  sitemap: https://other.test/sitemap.xml")]
    public void Editorial_Sitemap_Is_Not_Duplicated(string body) =>
        Assert.That(SeoController.AppendSitemapDirective(body, SITEMAP_URL), Is.EqualTo(body));

    [Test]
    public void Missing_Sitemap_Url_Leaves_Body_Unchanged() =>
        Assert.That(SeoController.AppendSitemapDirective("User-agent: *\n", null), Is.EqualTo("User-agent: *\n"));
}
