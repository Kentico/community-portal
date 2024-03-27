using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(SupportRequestConfigurationApplicationPage),
    slug: "edit",
    uiPageType: typeof(SupportRequestConfigurationEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestConfigurationEditPage(IInfoProvider<SupportRequestConfigurationInfo> configProvider, IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder) : InfoEditPage<SupportRequestConfigurationInfo>(formComponentMapper, formDataBinder)
{
    private readonly IInfoProvider<SupportRequestConfigurationInfo> configProvider = configProvider;

    // Not used since we only edit the singleton configuration
    public override int ObjectId { get; set; }

    protected override async Task<SupportRequestConfigurationInfo> GetInfoObject(CancellationToken? cancellationToken = null)
    {
        var config = (await configProvider.Get()
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault()!;

        ObjectId = config.SupportRequestConfigurationID;

        return config;
    }
}
