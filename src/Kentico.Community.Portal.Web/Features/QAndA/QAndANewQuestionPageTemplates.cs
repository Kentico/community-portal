using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.QAndANewQuestionPage_Default",
    name: "Q&A New Question Page - Default",
    propertiesType: typeof(QAndANewQuestionPageTemplateProperties),
    customViewName: "~/Features/QAndA/QAndANewQuestionPage_Default.cshtml",
    ContentTypeNames = new[] { QAndANewQuestionPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndANewQuestionPageTemplateProperties : IPageTemplateProperties
{

}
