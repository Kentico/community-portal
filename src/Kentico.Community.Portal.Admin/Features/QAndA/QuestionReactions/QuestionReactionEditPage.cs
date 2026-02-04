using Kentico.Community.Portal.Admin.Features.QAndA.QuestionReactions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(QuestionReactionSectionPage),
    slug: "edit",
    uiPageType: typeof(QuestionReactionEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.QuestionReactions;

public class QuestionReactionEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder)
    : InfoEditPage<QAndAQuestionReactionInfo>(formComponentMapper, formDataBinder)
{

    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }
}
