using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Sections.TwoColumn;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    identifier: TwoColumnSection.IDENTIFIER,
    viewComponentType: typeof(TwoColumnSection),
    name: "2 column - 25/75",
    propertiesType: typeof(TwoColumnSectionProperties),
    Description = "Two-column section with columns of different sizes, split 25/75.",
    IconClass = KenticoIcons.L_COLS_30_70)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Sections.TwoColumn;

public class TwoColumnSection : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Section_25_75";

    public IViewComponentResult Invoke() => View("~/Components/PageBuilder/Sections/TwoColumn/TwoColumn.cshtml");
}

public class TwoColumnSectionProperties : ISectionProperties
{

}
