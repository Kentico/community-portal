using Kentico.Community.Portal.Web.Components.Sections.SingleColumn;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    identifier: SingleColumnSection.IDENTIFIER,
    viewComponentType: typeof(SingleColumnSection),
    name: "1 column",
    propertiesType: typeof(SingleColumnSectionProperties),
    Description = "Single-column section with one full-width zone.",
    IconClass = "icon-square")]

namespace Kentico.Community.Portal.Web.Components.Sections.SingleColumn;

public class SingleColumnSection : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.SingleColumnSection";

    public IViewComponentResult Invoke() => View("~/Components/Sections/SingleColumn/SingleColumn.cshtml");
}

public class SingleColumnSectionProperties : ISectionProperties
{

}
