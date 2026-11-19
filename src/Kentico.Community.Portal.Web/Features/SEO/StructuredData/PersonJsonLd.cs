using System.Text.Json.Serialization;

namespace Kentico.Community.Portal.Web.Features.SEO.StructuredData;

/// <summary>
/// https://schema.org/Person, embedded in another node (article, question, answer) - never top-level,
/// because member profiles are not indexed.
/// Only emit values that are visibly rendered on the page.
/// </summary>
/// <param name="Name">Display name shown on the page</param>
/// <param name="URL">Absolute profile URL, when the page links to the profile</param>
public record PersonJsonLd(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string? URL = null)
{
    [JsonPropertyName("@type")]
    [JsonPropertyOrder(-1)]
    public string Type => "Person";
}
