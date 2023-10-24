using Kentico.Community.Admin.Features.QAndA;
using Kentico.Community.Portal.Core;
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

namespace Kentico.Community.Admin.Features.QAndA;

public class QAndACreatePage : CreatePage<QAndAAnswerDataInfo, QAndAEditPage>
{
    private readonly ISystemClock clock;

    public QAndACreatePage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IPageUrlGenerator pageUrlGenerator, ISystemClock clock)
        : base(formComponentMapper, formDataBinder, pageUrlGenerator) => this.clock = clock;

    protected override async Task FinalizeInfoObject(QAndAAnswerDataInfo infoObject, IFormFieldValueProvider fieldValueProvider, CancellationToken cancellationToken)
    {
        await base.FinalizeInfoObject(infoObject, fieldValueProvider, cancellationToken);

        infoObject.QAndAAnswerDataDateModified = infoObject.QAndAAnswerDataDateCreated = clock.UtcNow;
    }
}
