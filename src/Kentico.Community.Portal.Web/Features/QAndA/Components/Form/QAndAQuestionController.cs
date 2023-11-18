using CMS.Membership;
using CMS.Websites.Routing;
using Htmx;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Admin.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

[Authorize]
[Route("[controller]/[action]")]
public class QAndAQuestionController : Controller
{
    private readonly UserManager<CommunityMember> userManager;
    private readonly IUserInfoProvider userInfoProvider;
    private readonly IMediator mediator;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IWebsiteChannelContext channelContext;

    public QAndAQuestionController(
        UserManager<CommunityMember> userManager,
        IUserInfoProvider userInfoProvider,
        IMediator mediator,
        IWebPageUrlRetriever urlRetriever,
        IWebsiteChannelContext channelContext)
    {
        this.userManager = userManager;
        this.userInfoProvider = userInfoProvider;
        this.mediator = mediator;
        this.urlRetriever = urlRetriever;
        this.channelContext = channelContext;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(QAndAQuestionFormSubmissionViewModel requestModel)
    {
        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAQuestionFormViewComponent));
        }

        var member = await userManager.CurrentUser(HttpContext)!;
        var questionParent = await mediator.Send(new QAndAQuestionsRootPageQuery());

        _ = await mediator.Send(new QAndAQuestionCreateCommand(
            member,
            questionParent,
            channelContext.WebsiteChannelID,
            requestModel.Title,
            requestModel.Content));

        Response.Htmx(h => h.Redirect("/q-and-a"));

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuestion(QAndAQuestionFormSubmissionViewModel requestModel)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("~/Features/QAndA/Components/Form/QAndAQuestionForm.cshtml", new QAndAQuestionFormViewModel());
        }

        if (requestModel.EditedObjectID is not Guid questionID)
        {
            return NotFound();
        }

        var user = await userManager.CurrentUser(HttpContext)!;

        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (question is null)
        {
            return NotFound();
        }

        _ = await mediator.Send(new QAndAQuestionUpdateCommand(question, requestModel.Title, requestModel.Content));

        string redirectURL = (await urlRetriever.Retrieve(question)).RelativePathTrimmed();

        Response.Htmx(h => h.Redirect(redirectURL));

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> DisplayEditQuestionForm(Guid questionID)
    {
        var user = await userManager.CurrentUser(HttpContext);

        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (question is null)
        {
            return NotFound();
        }

        bool canManageContent = await userManager.CanManageContent(user, userInfoProvider);

        if (question.QAndAQuestionPageAuthorMemberID != user.Id && !canManageContent)
        {
            return Forbid();
        }

        return ViewComponent(typeof(QAndAQuestionFormViewComponent), new { questionID });
    }


    [ValidateAntiForgeryToken]
    [HttpDelete]
    public async Task<IActionResult> DeleteQuestion(Guid questionID)
    {
        /**
         * For now, only content managers can delete questions
         */
        bool canManageContent = await userManager.CanManageContent(HttpContext, userInfoProvider);

        if (!canManageContent)
        {
            return Forbid();
        }

        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (question is null)
        {
            return NotFound();
        }

        _ = await mediator.Send(new QAndAQuestionDeleteCommand(question));

        return Ok();
    }
}
