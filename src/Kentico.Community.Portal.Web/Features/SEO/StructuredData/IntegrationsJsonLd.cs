using System.Text.Json.Serialization;

namespace Kentico.Community.Portal.Web.Features.SEO.StructuredData;

/// <summary>
/// https://schema.org/ItemList of the integrations hub entries, in rendered order.
/// </summary>
public record ItemListJsonLd(
    [property: JsonPropertyName("itemListElement")] IReadOnlyList<SoftwareListItemJsonLd> ItemListElement) : JsonLdNode("ItemList");

/// <summary>
/// https://schema.org/ListItem, embedded in <see cref="ItemListJsonLd"/>.
/// </summary>
/// <param name="Position">1-based position in the rendered list</param>
/// <param name="Item">The listed integration</param>
public record SoftwareListItemJsonLd(
    [property: JsonPropertyName("position")] int Position,
    [property: JsonPropertyName("item")] SoftwareApplicationJsonLd Item)
{
    [JsonPropertyName("@type")]
    [JsonPropertyOrder(-1)]
    public string Type => "ListItem";
}

/// <summary>
/// https://schema.org/SoftwareApplication, embedded in <see cref="SoftwareListItemJsonLd"/>.
/// Only emit values that are visibly rendered on the page.
/// </summary>
/// <param name="Name">Title shown on the entry</param>
/// <param name="URL">Absolute URL the entry links to (repository, otherwise library, otherwise the entry permalink)</param>
/// <param name="Description">Short description shown on the entry</param>
/// <param name="ApplicationCategory">Integration type shown on the entry</param>
/// <param name="Author">Member author shown on the entry</param>
public record SoftwareApplicationJsonLd(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string URL,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("applicationCategory")] string? ApplicationCategory = null,
    [property: JsonPropertyName("author")] PersonJsonLd? Author = null)
{
    [JsonPropertyName("@type")]
    [JsonPropertyOrder(-1)]
    public string Type => "SoftwareApplication";
}
