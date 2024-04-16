using CMS.Membership;
using CMS.Websites.Routing;
using Htmx;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

[Authorize]
[Route("[controller]/[action]")]
public class QAndAAnswerController(
    UserManager<CommunityMember> userManager,
    IWebPageUrlRetriever urlRetriever,
    IUserInfoProvider userInfoProvider,
    IMediator mediator,
    IWebsiteChannelContext channelContext
    ) : Controller
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IUserInfoProvider userInfoProvider = userInfoProvider;
    private readonly IMediator mediator = mediator;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateAnswer(QAndAAnsweredViewModel requestModel)
    {
        var questionID = requestModel.ParentQuestionID;

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID });
        }

        var parentQuestion = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));
        if (parentQuestion is null)
        {
            ModelState.AddModelError(nameof(questionID), "Question is not valid");

            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID });
        }

        var member = await userManager.CurrentUser(HttpContext);

        if (member is null)
        {
            return Unauthorized();
        }

        _ = await mediator.Send(new QAndAAnswerCreateCommand(member, requestModel.Content, parentQuestion, channelContext));

        string questionPath = (await urlRetriever.Retrieve(parentQuestion)).RelativePathTrimmed();

        Response.Htmx(h => h.Redirect(questionPath));

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateAnswer(QAndAAnsweredViewModel requestModel)
    {
        var questionID = requestModel.ParentQuestionID;

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID, answerID = requestModel.EditedObjectID });
        }

        if (requestModel.EditedObjectID is not int answerID)
        {
            return NotFound();
        }

        var parentQuestion = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));
        if (parentQuestion is null)
        {
            ModelState.AddModelError(nameof(questionID), "Question is not valid");

            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID, answerID });
        }

        var user = await userManager.CurrentUser(HttpContext)!;
        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));
        if (answer is null)
        {
            return NotFound();
        }

        _ = await mediator.Send(new QAndAAnswerUpdateCommand(answer, requestModel.Content));

        string questionPath = (await urlRetriever.Retrieve(parentQuestion)).RelativePathTrimmed();

        Response.Htmx(h => h.Redirect(questionPath));

        return Ok();
    }

    [HttpGet]
    public ActionResult DisplayCreateAnswerForm(Guid questionID) =>
        ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID });

    [HttpGet]
    public async Task<ActionResult> DisplayEditAnswerForm(Guid questionID, int answerID)
    {
        var member = await userManager.CurrentUser(HttpContext);
        if (member is null)
        {
            return Unauthorized();
        }

        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));
        if (answer is null)
        {
            return NotFound();
        }

        bool canManageContent = await userManager.CanManageContent(member, userInfoProvider);
        if (answer.QAndAAnswerDataAuthorMemberID != member.Id && !canManageContent)
        {
            return Forbid();
        }

        return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID, answerID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> MarkApprovedAnswer(Guid questionID, int answerID)
    {
        var parentQuestion = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));
        if (parentQuestion is null)
        {
            return NotFound();
        }

        var member = await userManager.CurrentUser(HttpContext);
        if (member is null)
        {
            return Unauthorized();
        }

        bool canManageContent = await userManager.CanManageContent(member, userInfoProvider);
        if (parentQuestion.QAndAQuestionPageAuthorMemberID != member.Id && !canManageContent)
        {
            return Forbid();
        }

        if (parentQuestion.QAndAQuestionPageAcceptedAnswerDataGUID != default)
        {
            return Forbid();
        }

        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));

        _ = await mediator.Send(new QAndAQuestionMarkAnsweredCommand(parentQuestion, answer, channelContext.WebsiteChannelID));

        string questionPath = (await urlRetriever.Retrieve(parentQuestion)).RelativePathTrimmed();

        Response.Htmx(h => h.Redirect(questionPath));

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteAnswer(int answerID)
    {
        var member = await userManager.CurrentUser(HttpContext);
        if (member is null)
        {
            return Unauthorized();
        }

        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));
        if (answer is null)
        {
            return NotFound();
        }

        bool canManageContent = await userManager.CanManageContent(member, userInfoProvider);
        if (answer.QAndAAnswerDataAuthorMemberID != member.Id && !canManageContent)
        {
            return Forbid();
        }

        _ = await mediator.Send(new QAndAAnswerDeleteCommand(answer));

        return Ok();
    }
}
