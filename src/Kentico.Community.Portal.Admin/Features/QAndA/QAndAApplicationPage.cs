using Kentico.Community.Admin.Features.QAndA;
using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: QAndAApplicationPage.IDENTIFIER,
    type: typeof(QAndAApplicationPage),
    slug: "questions-answers",
    name: "Q&A",
    category: PortalWebAdminModule.CATEGORY,
    icon: Icons.RectangleParagraph,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Admin.Features.QAndA;

public class QAndAApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "q-and-a-app";
}
