using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.QAndA.Answers;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(AnswerDataListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(AnswerDataSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.Answers;

public class AnswerDataSectionPage : EditSectionPage<QAndAAnswerDataInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is not QAndAAnswerDataInfo answer)
        {
            return await base.GetObjectDisplayName(infoObject);
        }

        return string.IsNullOrWhiteSpace(answer.QAndAAnswerDataCodeName)
            ? await base.GetObjectDisplayName(infoObject)
            : answer.QAndAAnswerDataCodeName;
    }
}
