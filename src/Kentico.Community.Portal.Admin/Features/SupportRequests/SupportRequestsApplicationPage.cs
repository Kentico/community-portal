using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: SupportRequestsApplicationPage.IDENTIFIER,
    type: typeof(SupportRequestsApplicationPage),
    slug: "support-requests",
    name: "Support Requests",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.List,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "support-requests-app";
}
