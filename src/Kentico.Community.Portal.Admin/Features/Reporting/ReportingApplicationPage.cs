using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.Reporting;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: ReportingApplicationPage.IDENTIFIER,
    type: typeof(ReportingApplicationPage),
    slug: "reporting",
    name: "Reporting",
    category: PortalWebAdminModule.STATS_CATEGORY,
    icon: Icons.Graph,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.Reporting;

[UIPermission(SystemPermissions.VIEW)]
public class ReportingApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "reporting-app";
}
