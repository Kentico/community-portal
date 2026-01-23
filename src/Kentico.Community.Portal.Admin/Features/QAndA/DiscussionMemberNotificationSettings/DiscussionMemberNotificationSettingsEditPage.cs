using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberNotificationSettings;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(DiscussionMemberNotificationSettingsSectionPage),
    slug: "edit",
    uiPageType: typeof(DiscussionMemberNotificationSettingsEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberNotificationSettings;

public class DiscussionMemberNotificationSettingsEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IInfoProvider<MemberInfo> provider)
    : InfoEditPage<DiscussionMemberNotificationSettingsInfo>(formComponentMapper, formDataBinder)
{
    private readonly IInfoProvider<MemberInfo> provider = provider;

    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }

    protected override async Task<string> GetObjectDisplayName(DiscussionMemberNotificationSettingsInfo infoObject)
    {
        if (infoObject is not DiscussionMemberNotificationSettingsInfo settings)
        {
            return await base.GetObjectDisplayName(infoObject);
        }

        var member = (await provider.Get()
            .WhereEquals(nameof(MemberInfo.MemberID), settings.DiscussionMemberNotificationSettingsMemberID)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (member is null)
        {
            return await base.GetObjectDisplayName(infoObject);
        }

        return $"{member.MemberName} ({member.MemberEmail})";
    }
}
