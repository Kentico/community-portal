using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.QAndALandingPage_Default",
    name: "Q&A Landing Page - Default",
    propertiesType: typeof(QAndALandingPageTemplateProperties),
    customViewName: "~/Features/QAndA/QAndALandingPage_Default.cshtml",
    ContentTypeNames = new[] { QAndALandingPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndALandingPageTemplateProperties : IPageTemplateProperties
{
    [CheckBoxComponent(
        Label = "Display page description",
        ExplanationText = "If true, the Short Description of the page is displayed",
        Order = 1
    )]
    public bool DisplayPageDescription { get; set; } = true;
}
