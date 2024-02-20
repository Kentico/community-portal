using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: SupportRequestConfigurationApplicationPage.IDENTIFIER,
    type: typeof(SupportRequestConfigurationApplicationPage),
    slug: "support-request-configuration",
    name: "Configuration",
    category: PortalWebAdminModule.SUPPORT_REQUESTS_CATEGORY,
    icon: Icons.Cogwheels,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestConfigurationApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "support-request-configuration-app";
}
