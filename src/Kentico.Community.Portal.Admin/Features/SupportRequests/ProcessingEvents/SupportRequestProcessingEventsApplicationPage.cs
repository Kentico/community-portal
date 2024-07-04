using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: SupportRequestProcessingEventsApplicationPage.IDENTIFIER,
    type: typeof(SupportRequestProcessingEventsApplicationPage),
    slug: "support-request-processing-events",
    name: "Support Requests",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.List,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestProcessingEventsApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "support-request-processing-events-app";

    public override async Task<TemplateClientProperties> ConfigureTemplateProperties(TemplateClientProperties properties)
    {
        var props = await base.ConfigureTemplateProperties(properties);
        props.Navigation.Items = [];
        props.Breadcrumbs.Label = "Support Request Processing Events";

        return props;
    }
}
