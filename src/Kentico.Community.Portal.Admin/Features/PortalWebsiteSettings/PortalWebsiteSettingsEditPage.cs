using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.PortalWebsiteSettings;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(PortalWebsiteSettingsApplicationPage),
    slug: "config",
    uiPageType: typeof(PortalWebsiteSettingsEditPage),
    name: "Configuration",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.PortalWebsiteSettings;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class PortalWebsiteSettingsEditPage(
    IInfoProvider<PortalWebsiteSettingsInfo> configProvider,
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    TimeProvider time)
    : InfoEditPage<PortalWebsiteSettingsInfo>(formComponentMapper, formDataBinder)
{
    private readonly IInfoProvider<PortalWebsiteSettingsInfo> configProvider = configProvider;
    private readonly TimeProvider time = time;

    // Not used since we only edit the singleton configuration
    public override int ObjectId { get; set; }

    protected override async Task<PortalWebsiteSettingsInfo> GetInfoObject(CancellationToken? cancellationToken = null)
    {
        var config = (await configProvider.Get()
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (config is null)
        {
            var now = time.GetLocalNow().DateTime;

            config = new PortalWebsiteSettingsInfo
            {
                PortalWebsiteSettingsWebsiteAlertSettingsContent = [],
                PortalWebsiteSettingsWebsiteCookieBannerContent = [],
                PortalWebsiteSettingsWebsiteGlobalContent = [],
                PortalWebsiteSettingsDateCreated = now,
                PortalWebsiteSettingsName = "PortalWebsiteSettings"
            };
            config.Insert();
        }

        ObjectId = config.PortalWebsiteSettingsID;

        return config;
    }
}
