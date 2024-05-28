using CMS.DataEngine;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: SupportRequestConfigurationApplicationPage.IDENTIFIER,
    type: typeof(SupportRequestConfigurationApplicationPage),
    slug: "support-request-configuration",
    name: "Configuration",
    category: PortalWebAdminModule.SUPPORT_REQUESTS_CATEGORY,
    icon: Icons.Cogwheels,
    templateName: TemplateNames.SIDE_NAVIGATION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestConfigurationApplicationPage(
    IInfoProvider<SupportRequestConfigurationInfo> configProvider,
    IPageUrlGenerator urlGenerator)
    : ApplicationPage
{
    public const string IDENTIFIER = "support-request-configuration-app";

    private readonly IInfoProvider<SupportRequestConfigurationInfo> configProvider = configProvider;
    private readonly IPageUrlGenerator urlGenerator = urlGenerator;

    public override async Task<TemplateClientProperties> ConfigureTemplateProperties(TemplateClientProperties properties)
    {
        var props = await base.ConfigureTemplateProperties(properties);
        props.Navigation.Items = [];
        props.Breadcrumbs.Label = "Support Request Configuration";

        return props;
    }

    protected override Route GetDefaultRoute(IEnumerable<Route> routes)
    {
        var route = new Route()
        {
            PageLocationConfiguration = new()
            {
                Location = PageLocationEnum.MainContent
            },
            TemplateName = TemplateNames.EDIT
        };

        var config = configProvider.Get().GetEnumerableTypedResult().FirstOrDefault();

        if (config is null)
        {
            route.Path = urlGenerator.GenerateUrl(typeof(SupportRequestConfigurationCreatePage));
            return route;
        }

        route.Path = urlGenerator.GenerateUrl(typeof(SupportRequestConfigurationEditPage)); // , [config.SupportRequestConfigurationID.ToString()]

        return route;
    }
}
