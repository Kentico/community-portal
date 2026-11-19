using Kentico.Community.Portal.Web.Features.SEO.StructuredData;

namespace Kentico.Community.Portal.Web.Tests.Features.SEO;

public class BlogPostingJsonLdTests
{
    [Test]
    public void Serializes_Headline_Author_Date_And_URL()
    {
        string json = JsonLdSerializer.Serialize(BlogPostingJsonLd.Create(
            "Post",
            new DateTime(2024, 12, 12, 9, 5, 0, DateTimeKind.Utc),
            new PersonJsonLd("Jane Doe", "https://example.com/members/1"),
            "https://example.com/blog/post"));

        Assert.That(json, Is.EqualTo(
            """{"@context":"https://schema.org","@type":"BlogPosting","headline":"Post","author":{"@type":"Person","name":"Jane Doe","url":"https://example.com/members/1"},"datePublished":"2024-12-12T09:05:00Z","url":"https://example.com/blog/post","mainEntityOfPage":"https://example.com/blog/post"}"""));
    }

    [Test]
    public void Omits_Author_URL_When_Profile_Not_Linked()
    {
        string json = JsonLdSerializer.Serialize(BlogPostingJsonLd.Create(
            "Post", new DateTime(2024, 12, 12), new PersonJsonLd("Kentico"), "https://example.com/blog/post"));

        Assert.That(json, Does.Contain("""{"@type":"Person","name":"Kentico"}"""));
    }
}
