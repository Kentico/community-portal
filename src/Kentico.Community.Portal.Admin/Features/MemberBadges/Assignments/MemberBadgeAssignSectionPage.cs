using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(MemberBadgeAssignmentListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(MemberBadgeAssignSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

public class MemberBadgeAssignSectionPage : EditSectionPage<MemberInfo>
{
}
