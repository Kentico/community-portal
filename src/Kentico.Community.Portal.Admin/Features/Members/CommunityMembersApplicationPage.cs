using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: CommunityMembersApplicationPage.IDENTIFIER,
    type: typeof(CommunityMembersApplicationPage),
    slug: "community-members",
    name: "Community Members",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.PersonalisationVariants,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.Members;

[UIPermission(SystemPermissions.VIEW)]
public class CommunityMembersApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "community-members-app";
}
