using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: SupportRequestProcessingEventsApplicationPage.IDENTIFIER,
    type: typeof(SupportRequestProcessingEventsApplicationPage),
    slug: "support-request-processing-events",
    name: "Processing Events",
    category: PortalWebAdminModule.SUPPORT_REQUESTS_CATEGORY,
    icon: Icons.List,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestProcessingEventsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "support-request-processing-events-app";
}
