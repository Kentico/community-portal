using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.Migrations;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: MigrationsApplicationPage.IDENTIFIER,
    type: typeof(MigrationsApplicationPage),
    slug: "migrations",
    name: "Migrations",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.Database,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.Migrations;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.UPDATE)]
public class MigrationsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "migrations-app";
}
