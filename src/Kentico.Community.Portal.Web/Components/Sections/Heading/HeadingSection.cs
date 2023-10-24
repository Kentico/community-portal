using Kentico.Community.Portal.Web.Components.Sections.Heading;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    identifier: HeadingSection.IDENTIFIER,
    viewComponentType: typeof(HeadingSection),
    name: "Heading",
    propertiesType: typeof(HeadingSectionProperties),
    Description = "Heading Section to be used at the top of a page",
    IconClass = "icon-i"
)]

namespace Kentico.Community.Portal.Web.Components.Sections.Heading;

public class HeadingSection : ViewComponent
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Web.Sections.Heading";

    public IViewComponentResult Invoke() => View("~/Components/Sections/Heading/HeadingSection.cshtml");
}

public class HeadingSectionProperties : ISectionProperties
{

}
