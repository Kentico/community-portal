using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.WebsiteChannels;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: WebsiteChannelsApplicationPage.IDENTIFIER,
    type: typeof(WebsiteChannelsApplicationPage),
    slug: "website-channels",
    name: "Website channels",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.Earth,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.WebsiteChannels;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class WebsiteChannelsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "website-channels-app";
}
