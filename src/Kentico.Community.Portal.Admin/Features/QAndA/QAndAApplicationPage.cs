using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: QAndAApplicationPage.IDENTIFIER,
    type: typeof(QAndAApplicationPage),
    slug: "q-and-a",
    name: "Q&A",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.RectangleParagraph,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.QAndA;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class QAndAApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "q-and-a-app";
}
