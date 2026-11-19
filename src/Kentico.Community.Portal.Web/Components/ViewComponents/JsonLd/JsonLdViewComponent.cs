using Kentico.Community.Portal.Web.Features.SEO.StructuredData;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.JsonLd;

/// <summary>
/// Renders the JSON-LD nodes added to <see cref="JsonLdCollector"/> for the current request.
/// </summary>
public class JsonLdViewComponent(JsonLdCollector collector) : ViewComponent
{
    private readonly JsonLdCollector collector = collector;

    public IViewComponentResult Invoke() =>
        View("~/Components/ViewComponents/JsonLd/JsonLd.cshtml",
            collector.Nodes.Select(JsonLdSerializer.Serialize).ToList());
}
