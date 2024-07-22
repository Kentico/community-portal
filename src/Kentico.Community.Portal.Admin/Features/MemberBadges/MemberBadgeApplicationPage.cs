using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: MemberBadgeApplicationPage.IDENTIFIER,
    type: typeof(MemberBadgeApplicationPage),
    slug: "memberbadges",
    name: "Member Badges",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.Badge,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
internal class MemberBadgeApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Web.Admin.Member.Badges";
}
