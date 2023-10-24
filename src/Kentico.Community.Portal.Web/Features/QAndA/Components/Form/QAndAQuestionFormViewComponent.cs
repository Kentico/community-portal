using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndAQuestionFormViewComponent : ViewComponent
{
    private readonly IMediator mediator;

    public QAndAQuestionFormViewComponent(IMediator mediator) => this.mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(Guid? questionID = null)
    {
        var rootPage = await mediator.Send(new QAndALandingPageQuery());

        if (rootPage is null)
        {
            return View("~/Components/ComponentError.cshtml");
        }

        var vm = new QAndAQuestionFormViewModel
        {
            FormHelpMessageHTML = new(rootPage.QAndALandingPageMarkdownFormHelpMessageHTML)
        };

        if (questionID is Guid id)
        {
            var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(id));
            if (question is null)
            {
                return View("~/Components/ComponentError.cshtml");
            }

            vm.EditedObjectID = id;
            vm.Title = question.QAndAQuestionPageTitle;
            vm.Content = question.QAndAQuestionPageContent;
        }

        return View("~/Features/QAndA/Components/Form/QAndAQuestionForm.cshtml", vm);
    }
}

public class QAndAQuestionFormViewModel
{
    [Required(ErrorMessage = "A Title is required")]
    [MaxLength(100, ErrorMessage = "Title can be at most 100 characters")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = "";

    [HiddenInput]
    public Guid? EditedObjectID { get; set; }

    public HtmlString FormHelpMessageHTML { get; set; } = HtmlString.Empty;
}

public class QAndAQuestionFormSubmissionViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(100, ErrorMessage = "Title can be at most 100 characters")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = "";

    public Guid? EditedObjectID { get; set; }
}
