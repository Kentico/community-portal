using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberNotificationSettings;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(DiscussionMemberNotificationSettingsListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(DiscussionMemberNotificationSettingsSectionPage),
    name: "Section",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberNotificationSettings;

public class DiscussionMemberNotificationSettingsSectionPage(IInfoProvider<MemberInfo> provider) : EditSectionPage<DiscussionMemberNotificationSettingsInfo>
{
    private readonly IInfoProvider<MemberInfo> provider = provider;

    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
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
