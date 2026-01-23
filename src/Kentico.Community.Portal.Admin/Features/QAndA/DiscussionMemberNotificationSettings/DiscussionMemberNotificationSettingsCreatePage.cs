using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberNotificationSettings;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(DiscussionMemberNotificationSettingsListingPage),
    slug: "create",
    uiPageType: typeof(DiscussionMemberNotificationSettingsCreatePage),
    name: "Create",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberNotificationSettings;

public class DiscussionMemberNotificationSettingsCreatePage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IPageLinkGenerator pageLinkGenerator, TimeProvider clock)
    : CreatePage<DiscussionMemberNotificationSettingsInfo, DiscussionMemberNotificationSettingsEditPage>(formComponentMapper, formDataBinder, pageLinkGenerator)
{
    private readonly TimeProvider clock = clock;

    protected override async Task FinalizeInfoObject(DiscussionMemberNotificationSettingsInfo infoObject, IFormFieldValueProvider fieldValueProvider, CancellationToken cancellationToken)
    {
        await base.FinalizeInfoObject(infoObject, fieldValueProvider, cancellationToken);

        infoObject.DiscussionMemberNotificationSettingsDateModified = infoObject.DiscussionMemberNotificationSettingsDateCreated = clock.GetLocalNow().DateTime;
    }
}
