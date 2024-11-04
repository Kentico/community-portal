using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.PortalWebsiteSettings;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: PortalWebsiteSettingsApplicationPage.IDENTIFIER,
    type: typeof(PortalWebsiteSettingsApplicationPage),
    slug: "portal-website-settings",
    name: "Portal Website Settings",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.CogwheelSquare,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.PortalWebsiteSettings;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
public class PortalWebsiteSettingsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "portal-website-settings-app";
}
