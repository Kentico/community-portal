using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(SupportRequestsApplicationPage),
    slug: "config",
    uiPageType: typeof(SupportRequestConfigurationEditPage),
    name: "Configuration",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class SupportRequestConfigurationEditPage(IInfoProvider<SupportRequestConfigurationInfo> configProvider, IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder)
    : InfoEditPage<SupportRequestConfigurationInfo>(formComponentMapper, formDataBinder)
{
    private readonly IInfoProvider<SupportRequestConfigurationInfo> configProvider = configProvider;

    // Not used since we only edit the singleton configuration
    public override int ObjectId { get; set; }

    protected override async Task<SupportRequestConfigurationInfo> GetInfoObject(CancellationToken? cancellationToken = null)
    {
        var config = (await configProvider.Get()
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (config is null)
        {
            config = new SupportRequestConfigurationInfo
            {
                SupportRequestConfigurationExternalEndpointURL = "",
                SupportRequestConfigurationIsQueueProcessingEnabled = false,
            };
            config.Insert();
        }

        ObjectId = config.SupportRequestConfigurationID;

        return config;
    }
}
