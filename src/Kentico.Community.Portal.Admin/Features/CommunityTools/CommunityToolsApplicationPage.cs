using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.CommunityTools;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: CommunityToolsApplicationPage.IDENTIFIER,
    type: typeof(CommunityToolsApplicationPage),
    slug: "community-tools",
    name: "Community Tools",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.Cogwheels,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.CommunityTools;

[UIPermission(SystemPermissions.VIEW)]
public class CommunityToolsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "community-tools-app";
}
