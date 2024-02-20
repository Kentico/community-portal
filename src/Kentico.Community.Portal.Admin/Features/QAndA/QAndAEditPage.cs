using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(QAndASectionPage),
    slug: "edit",
    uiPageType: typeof(QAndAEditPage),
    name: "Edit Answer",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA;

public class QAndAEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, ISystemClock clock) : InfoEditPage<QAndAAnswerDataInfo>(formComponentMapper, formDataBinder)
{
    private readonly ISystemClock clock = clock;

    // Property that holds the ID of the edited user.
    // Needs to be decorated with the PageParameter attribute to propagate
    // user ID from the request path to the configuration of the page.
    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }

    protected override async Task FinalizeInfoObject(QAndAAnswerDataInfo infoObject, IFormFieldValueProvider fieldValueProvider, CancellationToken cancellationToken)
    {
        await base.FinalizeInfoObject(infoObject, fieldValueProvider, cancellationToken);

        infoObject.QAndAAnswerDataDateModified = clock.UtcNow;
    }
}
