using System.ComponentModel.DataAnnotations;
using CMS.Websites.Routing;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndAQuestionFormViewComponent(IMediator mediator, IWebsiteChannelContext channelContext) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    public async Task<IViewComponentResult> InvokeAsync(Guid? questionID = null)
    {
        var landingResp = await mediator.Send(new QAndALandingPageQuery(channelContext.WebsiteChannelName));

        if (!landingResp.TryGetValue(out var landingPage))
        {
            return View("~/Components/ComponentError.cshtml");
        }

        var formHelpMessageHTML = new HtmlString(landingPage.QAndALandingPageMarkdownFormHelpMessageHTML);

        if (questionID is not Guid id)
        {
            return View("~/Features/QAndA/Components/Form/QAndAQuestionForm.cshtml", QAndAQuestionFormViewModel.ForNewQuestion(formHelpMessageHTML));
        }

        var questionResp = await mediator.Send(new QAndAQuestionPageByGUIDQuery(id, channelContext.WebsiteChannelName));
        if (!questionResp.TryGetValue(out var questionPage))
        {
            return View("~/Components/ComponentError.cshtml");
        }

        var vm = new QAndAQuestionFormViewModel(id, questionPage.QAndAQuestionPageTitle, questionPage.QAndAQuestionPageContent, formHelpMessageHTML, []);

        return View("~/Features/QAndA/Components/Form/QAndAQuestionForm.cshtml", vm);
    }
}

public class QAndAQuestionFormViewModel
{
    [Required(ErrorMessage = "A Title is required")]
    [MaxLength(100, ErrorMessage = "Title can be at most 100 characters")]
    public string Title { get; }

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; }

    [HiddenInput]
    public Guid? EditedObjectID { get; }

    public HtmlString FormHelpMessageHTML { get; }
    public IReadOnlyList<string> DXTopics { get; }

    public static QAndAQuestionFormViewModel ForNewQuestion(HtmlString formHelpMessageHTML) => new(formHelpMessageHTML);
    public static QAndAQuestionFormViewModel FromFormSubmission(QAndAQuestionFormSubmissionViewModel vm, HtmlString formHelpMessageHTML) =>
        new(vm.EditedObjectID, vm.Title, vm.Content, formHelpMessageHTML, vm.DXTopics);

    private QAndAQuestionFormViewModel(HtmlString formHelpMessageHTML)
    {
        Title = "";
        Content = "";
        DXTopics = [];
        FormHelpMessageHTML = formHelpMessageHTML;
    }

    public QAndAQuestionFormViewModel(Guid? editedObjectID, string title, string content, HtmlString formHelpMessageHTML, IReadOnlyList<string> dxTopics)
    {
        EditedObjectID = editedObjectID;
        Title = title;
        Content = content;
        FormHelpMessageHTML = formHelpMessageHTML;
        DXTopics = dxTopics;
    }
}

public class QAndAQuestionFormSubmissionViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(100, ErrorMessage = "Title can be at most 100 characters")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = "";

    public Guid? EditedObjectID { get; set; }
    public IReadOnlyList<string> DXTopics { get; set; } = [];
}
