using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(MemberBadgeApplicationPage),
    slug: "config",
    uiPageType: typeof(MemberBadgeConfigurationEditPage),
    name: "Configuration",
    templateName: TemplateNames.EDIT,
    order: 20)]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

public class MemberBadgeConfigurationEditPage(IInfoProvider<MemberBadgeConfigurationInfo> configProvider, IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder)
    : InfoEditPage<MemberBadgeConfigurationInfo>(formComponentMapper, formDataBinder)
{
    private readonly IInfoProvider<MemberBadgeConfigurationInfo> configProvider = configProvider;

    // Not used since we only edit the singleton configuration
    public override int ObjectId { get; set; }

    protected override async Task<MemberBadgeConfigurationInfo> GetInfoObject(CancellationToken? cancellationToken = null)
    {
        var config = (await configProvider.Get()
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (config is null)
        {
            config = new MemberBadgeConfigurationInfo
            {
                MemberBadgeConfigurationGUID = Guid.NewGuid(),
                MemberBadgeConfigurationIsLoggingVerbose = false,
                MemberBadgeConfigurationNextRuleProcessDelaySeconds = 60,
                MemberBadgeConfigurationReExecuteLoopDelayMinutes = 60,
            };
            config.Insert();
        }

        ObjectId = config.MemberBadgeConfigurationID;

        return config;
    }
}
