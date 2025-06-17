using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.MemberBadges;

using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(MemberBadgeSectionPage),
    slug: "edit",
    uiPageType: typeof(MemberBadgeEditPage),
    name: "Edit Badge",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class MemberBadgeEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, TimeProvider clock)
    : InfoEditPage<MemberBadgeInfo>(formComponentMapper, formDataBinder)
{
    private readonly TimeProvider clock = clock;

    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }

    protected override async Task FinalizeInfoObject(MemberBadgeInfo infoObject, IFormFieldValueProvider fieldValueProvider, CancellationToken cancellationToken)
    {
        await base.FinalizeInfoObject(infoObject, fieldValueProvider, cancellationToken);

        infoObject.MemberBadgeDateModified = clock.GetUtcNow().DateTime;
    }
}

