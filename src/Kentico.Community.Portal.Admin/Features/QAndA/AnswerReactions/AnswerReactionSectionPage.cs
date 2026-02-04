using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.QAndA.AnswerReactions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(AnswerReactionListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(AnswerReactionSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.AnswerReactions;

public class AnswerReactionSectionPage : EditSectionPage<QAndAAnswerReactionInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is not QAndAAnswerReactionInfo reaction)
        {
            return await base.GetObjectDisplayName(infoObject);
        }

        return $"{reaction.QAndAAnswerReactionType} by Member {reaction.QAndAAnswerReactionMemberID}";
    }
}
