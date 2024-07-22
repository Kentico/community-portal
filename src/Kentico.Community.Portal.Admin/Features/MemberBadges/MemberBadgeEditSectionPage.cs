using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(MemberBadgeListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(MemberBadgeSectionPage),
    name: "Edit Section",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

public class MemberBadgeSectionPage : EditSectionPage<MemberBadgeInfo>
{
}
