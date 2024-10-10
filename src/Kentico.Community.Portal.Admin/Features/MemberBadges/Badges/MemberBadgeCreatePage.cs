using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(MemberBadgeListingPage),
    slug: "create",
    uiPageType: typeof(MemberBadgeCreatePage),
    name: "Create Badge",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

[UIEvaluatePermission(SystemPermissions.CREATE)]
public class MemberBadgeCreatePage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator,
    ISystemClock clock)
    : CreatePage<MemberBadgeInfo, MemberBadgeSectionPage>(formComponentMapper, formDataBinder, pageLinkGenerator)
{
    private readonly ISystemClock clock = clock;

    protected override async Task FinalizeInfoObject(MemberBadgeInfo infoObject, IFormFieldValueProvider fieldValueProvider, CancellationToken cancellationToken)
    {
        await base.FinalizeInfoObject(infoObject, fieldValueProvider, cancellationToken);

        infoObject.MemberBadgeDateModified = infoObject.MemberBadgeDateCreated = clock.UtcNow;
    }
}
