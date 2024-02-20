using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(SupportRequestConfigurationApplicationPage),
    slug: "list",
    uiPageType: typeof(SupportRequestConfigurationListingPage),
    name: "List",
    templateName: TemplateNames.LISTING,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestConfigurationListingPage(IInfoProvider<SupportRequestConfigurationInfo> configurationProvider) : ListingPage
{
    private readonly IInfoProvider<SupportRequestConfigurationInfo> configurationProvider = configurationProvider;

    protected override string ObjectType => SupportRequestConfigurationInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var configuration = (await configurationProvider.Get().TopN(1).GetEnumerableTypedResultAsync()).FirstOrDefault();

        if (configuration is null)
        {
            PageConfiguration.HeaderActions.AddLink<SupportRequestConfigurationCreatePage>("Create Configuration");
        }

        PageConfiguration.AddEditRowAction<SupportRequestConfigurationSectionPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(SupportRequestConfigurationInfo.SupportRequestConfigurationID),
                "ID",
                searchable: true)
            .AddColumn(nameof(SupportRequestConfigurationInfo.SupportRequestConfigurationIsQueueProcessingEnabled),
                "Queue Processing Enabled?",
                searchable: true)
            .AddColumn(
                nameof(SupportRequestConfigurationInfo.SupportRequestConfigurationLastModified),
                "Last Modified",
                searchable: true);
    }
}

