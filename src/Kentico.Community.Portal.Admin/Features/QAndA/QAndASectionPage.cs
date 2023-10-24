using CMS.DataEngine;
using Kentico.Community.Admin.Features.QAndA;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(QAndAListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(QAndASectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Admin.Features.QAndA;

public class QAndASectionPage : EditSectionPage<QAndAAnswerDataInfo>
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
