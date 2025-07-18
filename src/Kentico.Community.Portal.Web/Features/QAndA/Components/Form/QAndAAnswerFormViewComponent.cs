using System.ComponentModel.DataAnnotations;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndAAnswerFormViewComponent(
    IMediator mediator,
    IContentRetriever contentRetriever) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(Guid questionID, int? answerID = null)
    {
        var pages = await contentRetriever.RetrievePages<QAndALandingPage>();
        if (pages.FirstOrDefault() is not QAndALandingPage page)
        {
            return View("~/Components/ComponentError.cshtml");
        }

        var model = new QAndAAnswerViewModel
        {
            ParentQuestionID = questionID,
            FormHelpMessageHTML = new(page.QAndALandingPageMarkdownFormHelpMessageHTML)
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
