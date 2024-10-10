using Kentico.Community.Portal.Web.Components.Sections.Unstyled;
using Kentico.PageBuilder.Web.Mvc;

[assembly: RegisterSection(
    identifier: UnstyledSection.IDENTIFIER,
    name: "Unstyled",
    propertiesType: null,
    customViewName: "~/Components/Sections/Unstyled/Unstyled.cshtml",
    Description = "A completely unstyled section with a single Widget Zone. Can be used as a placeholder section.",
    IconClass = "icon-rectangle-o-h"
)]

namespace Kentico.Community.Portal.Web.Components.Sections.Unstyled;

public class UnstyledSection
{
    public const string IDENTIFIER = "KenticoCommunity.Section.Unstyled";
}
