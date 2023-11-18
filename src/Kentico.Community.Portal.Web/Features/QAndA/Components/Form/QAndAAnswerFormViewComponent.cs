using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndAAnswerFormViewComponent : ViewComponent
{
    private readonly IMediator mediator;

    public QAndAAnswerFormViewComponent(IMediator mediator) => this.mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(Guid questionID, int? answerID = null)
    {
        var rootPage = await mediator.Send(new QAndALandingPageQuery());

        if (rootPage is null)
        {
            return View("~/Components/ComponentError.cshtml");
        }

        var model = new QAndAAnswerViewModel
        {
            ParentQuestionID = questionID,
            FormHelpMessageHTML = new(rootPage.QAndALandingPageMarkdownFormHelpMessageHTML)
        };

        if (answerID is int id)
        {
            var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(id));

            if (answer is null)
            {
                return View("~/Components/ComponentError.cshtml");
            }

            model.EditedObjectID = id;
            model.Content = answer.QAndAAnswerDataContent;
        }

        return View("~/Features/QAndA/Components/Form/QAndAAnswerForm.cshtml", model);
    }
}

public class QAndAAnswerViewModel
{
    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = "";

    public HtmlString FormHelpMessageHTML { get; set; } = HtmlString.Empty;

    [HiddenInput]
    public Guid ParentQuestionID { get; set; }

    [HiddenInput]
    public int? EditedObjectID { get; set; }
}

public class QAndAAnsweredViewModel
{
    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = "";

    public int? EditedObjectID { get; set; }
    public Guid ParentQuestionID { get; set; }
}
