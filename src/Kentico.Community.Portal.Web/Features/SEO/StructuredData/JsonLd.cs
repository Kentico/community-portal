using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kentico.Community.Portal.Web.Features.SEO.StructuredData;

/// <summary>
/// A top-level schema.org JSON-LD object rendered in the page head.
/// Every value must be visibly rendered on the page it is emitted for.
/// </summary>
public abstract record JsonLdNode(
    [property: JsonPropertyName("@type"), JsonPropertyOrder(-1)] string Type)
{
    [JsonPropertyName("@context")]
    [JsonPropertyOrder(-2)]
    public string Context => "https://schema.org";
}

/// <summary>
/// https://schema.org/Organization
/// </summary>
/// <param name="Name">Organization name</param>
/// <param name="URL">Absolute URL of the site home page</param>
/// <param name="Logo">Absolute URL of the logo image</param>
public record OrganizationJsonLd(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string URL,
    [property: JsonPropertyName("logo")] string Logo) : JsonLdNode("Organization");

public static class JsonLdSerializer
{
    /// <summary>
    /// <see cref="System.Text.Encodings.Web.JavaScriptEncoder.Default"/> (the default) escapes
    /// <c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c> and quotes, so the output is safe as raw
    /// <c>&lt;script&gt;</c> content - <c>&lt;/script&gt;</c> cannot close the element.
    /// </summary>
    private static readonly JsonSerializerOptions options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(JsonLdNode node) =>
        JsonSerializer.Serialize(node, node.GetType(), options);
}
