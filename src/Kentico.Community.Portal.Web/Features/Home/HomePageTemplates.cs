using Kentico.Community.Portal.Web.Features.Home;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.HomePage_Default",
    name: "Home Page - Default",
    propertiesType: typeof(HomePageTemplateProperties),
    customViewName: "~/Features/Home/HomePage_Default.cshtml",
    ContentTypeNames = new[] { HomePage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.Home;

public class HomePageTemplateProperties : IPageTemplateProperties { }
