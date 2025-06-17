using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(QAndAListingPage),
    slug: "create",
    uiPageType: typeof(QAndACreatePage),
    name: "Create Answer",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA;

public class QAndACreatePage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IPageLinkGenerator pageLinkGenerator, TimeProvider clock)
    : CreatePage<QAndAAnswerDataInfo, QAndAEditPage>(formComponentMapper, formDataBinder, pageLinkGenerator)
{
    private readonly TimeProvider clock = clock;

    protected override async Task FinalizeInfoObject(QAndAAnswerDataInfo infoObject, IFormFieldValueProvider fieldValueProvider, CancellationToken cancellationToken)
    {
        await base.FinalizeInfoObject(infoObject, fieldValueProvider, cancellationToken);

        infoObject.QAndAAnswerDataDateModified = infoObject.QAndAAnswerDataDateCreated = clock.GetUtcNow().DateTime;
    }
}
