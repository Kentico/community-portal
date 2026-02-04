using Kentico.Community.Portal.Admin.Features.QAndA.AnswerReactions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(AnswerReactionSectionPage),
    slug: "edit",
    uiPageType: typeof(AnswerReactionEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.AnswerReactions;

public class AnswerReactionEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder)
    : InfoEditPage<QAndAAnswerReactionInfo>(formComponentMapper, formDataBinder)
{

    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }
}
