using System.Globalization;
using System.Text.Json.Serialization;

namespace Kentico.Community.Portal.Web.Features.SEO.StructuredData;

/// <summary>
/// https://schema.org/BlogPosting. Only values the blog post page renders are emitted:
/// no image (the featured image is head-only og:image) and no dateModified (not shown).
/// </summary>
/// <param name="Headline">Post title, rendered as the page h1</param>
/// <param name="Author">Author, rendered under the title and in the author card</param>
/// <param name="DatePublished">Published date, rendered in the title's <c>&lt;time&gt;</c></param>
/// <param name="URL">Canonical absolute URL of the post</param>
public record BlogPostingJsonLd(
    [property: JsonPropertyName("headline")] string Headline,
    [property: JsonPropertyName("author")] PersonJsonLd Author,
    [property: JsonPropertyName("datePublished")] string DatePublished,
    [property: JsonPropertyName("url")] string URL) : JsonLdNode("BlogPosting")
{
    [JsonPropertyName("mainEntityOfPage")]
    public string MainEntityOfPage => URL;

    /// <param name="published">Same value the page's <c>&lt;time datetime&gt;</c> renders</param>
    public static BlogPostingJsonLd Create(string headline, DateTime published, PersonJsonLd author, string canonicalURL) =>
        new(headline, author, published.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), canonicalURL);
}
