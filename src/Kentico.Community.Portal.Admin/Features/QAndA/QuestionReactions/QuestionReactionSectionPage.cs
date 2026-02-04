using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.QAndA.QuestionReactions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(QuestionReactionListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(QuestionReactionSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.QuestionReactions;

public class QuestionReactionSectionPage : EditSectionPage<QAndAQuestionReactionInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is not QAndAQuestionReactionInfo reaction)
        {
            return await base.GetObjectDisplayName(infoObject);
        }

        return $"{reaction.QAndAQuestionReactionType} by Member {reaction.QAndAQuestionReactionMemberID}";
    }
}
