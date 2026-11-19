namespace Kentico.Community.Portal.Web.Features.SEO.StructuredData;

/// <summary>
/// Request-scoped JSON-LD objects for the current page. Controllers <see cref="Add"/> nodes;
/// the page head renders each as a <c>&lt;script type="application/ld+json"&gt;</c> block.
/// </summary>
public class JsonLdCollector
{
    private readonly List<JsonLdNode> nodes = [];

    public IReadOnlyList<JsonLdNode> Nodes => nodes;

    public void Add(JsonLdNode node) => nodes.Add(node);
}
