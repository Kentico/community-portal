using Kentico.PageBuilder.Web.Mvc;

[assembly: RegisterSection(
    identifier: "KenticoCommunity.Section.Unstyled",
    name: "Unstyled",
    propertiesType: null,
    customViewName: "~/Components/Sections/Unstyled/Unstyled.cshtml",
    Description = "A completely unstyled section with a single Widget Zone. Can be used as a placeholder section.",
    IconClass = "icon-rectangle-o-h"
)]
