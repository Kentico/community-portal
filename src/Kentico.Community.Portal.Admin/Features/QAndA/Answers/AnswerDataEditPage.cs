using Kentico.Community.Portal.Admin.Features.QAndA.Answers;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(AnswerDataSectionPage),
    slug: "edit",
    uiPageType: typeof(AnswerDataEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.Answers;

public class AnswerDataEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, TimeProvider clock)
    : InfoEditPage<QAndAAnswerDataInfo>(formComponentMapper, formDataBinder)
{
    private readonly TimeProvider clock = clock;

    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }

    protected override async Task FinalizeInfoObject(QAndAAnswerDataInfo infoObject, IFormFieldValueProvider fieldValueProvider, CancellationToken cancellationToken)
    {
        await base.FinalizeInfoObject(infoObject, fieldValueProvider, cancellationToken);

        infoObject.QAndAAnswerDataDateModified = clock.GetUtcNow().DateTime;
    }
}
