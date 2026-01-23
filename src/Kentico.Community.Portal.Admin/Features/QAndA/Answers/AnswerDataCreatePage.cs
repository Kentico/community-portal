using Kentico.Community.Portal.Admin.Features.QAndA.Answers;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(AnswerDataListingPage),
    slug: "create",
    uiPageType: typeof(AnswerDataCreatePage),
    name: "Create",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.Answers;

public class AnswerDataCreatePage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IPageLinkGenerator pageLinkGenerator, TimeProvider clock)
    : CreatePage<QAndAAnswerDataInfo, AnswerDataEditPage>(formComponentMapper, formDataBinder, pageLinkGenerator)
{
    private readonly TimeProvider clock = clock;

    protected override async Task FinalizeInfoObject(QAndAAnswerDataInfo infoObject, IFormFieldValueProvider fieldValueProvider, CancellationToken cancellationToken)
    {
        await base.FinalizeInfoObject(infoObject, fieldValueProvider, cancellationToken);

        infoObject.QAndAAnswerDataDateModified = infoObject.QAndAAnswerDataDateCreated = clock.GetUtcNow().DateTime;
    }
}
