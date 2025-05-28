using CMS.Core;
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
    IWebsiteChannelContext channelContext,
    IEventLogService log) : PortalHandlerController(log)
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

        var parentQuestionResp = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (!parentQuestionResp.TryGetValue(out var parentQuestionPage))
        {
            ModelState.AddModelError(nameof(questionID), "Question is not valid");

            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID });
        }

        var member = await userManager.CurrentUser(HttpContext);
        if (member is null)
        {
            return Unauthorized();
        }

        return await mediator
            .Send(new QAndAAnswerCreateCommand(member, requestModel.Content, parentQuestionPage, channelContext))
            .Match(async id =>
            {
                string questionPath = (await urlRetriever.Retrieve(parentQuestionPage)).RelativePathTrimmed();
                Response.Htmx(h => h.Redirect(questionPath));

                return Ok().AsStatusCodeResult();

            }, LogAndReturnErrorAsync("ANSWER_CREATE"));
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

        var parentQuestionResp = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (!parentQuestionResp.TryGetValue(out var parentQuestionPage))
        {
            ModelState.AddModelError(nameof(questionID), "Question is not valid");

            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID, answerID });
        }

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

        return await mediator
            .Send(new QAndAAnswerUpdateCommand(answer, requestModel.Content))
            .Match(async () =>
            {
                string questionPath = (await urlRetriever.Retrieve(parentQuestionPage)).RelativePathTrimmed();
                Response.Htmx(h => h.Redirect(questionPath));

                return Ok().AsStatusCodeResult();
            }, LogAndReturnErrorAsync("ANSWSER_UPDATE"));
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
        var parentQuestionResp = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (!parentQuestionResp.TryGetValue(out var parentQuestionPage))
        {
            return NotFound();
        }

        var member = await userManager.CurrentUser(HttpContext);
        if (member is null)
        {
            return Unauthorized();
        }

        bool canManageContent = await userManager.CanManageContent(member, userInfoProvider);
        if (parentQuestionPage.QAndAQuestionPageAuthorMemberID != member.Id && !canManageContent)
        {
            return Forbid();
        }

        if (!(parentQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID == default || canManageContent))
        {
            return Forbid();
        }

        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));

        return await mediator
            .Send(new QAndAQuestionMarkAnsweredCommand(parentQuestionPage, answer, channelContext.WebsiteChannelID))
            .Match(async () =>
            {
                string questionPath = (await urlRetriever.Retrieve(parentQuestionPage)).RelativePathTrimmed();

                Response.Htmx(h => h.Redirect(questionPath));

                return Ok().AsStatusCodeResult();
            }, LogAndReturnErrorAsync("ANSWER_APPROVED"));
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

        return await mediator
            .Send(new QAndAAnswerDeleteCommand(answer))
            .Match(() =>
            {
                Response.Htmx(h => h.WithToastSuccess("Answer successfully deleted."));
                return Ok().AsStatusCodeResult();
            }, LogAndReturnError("ANSWER_DELETE"));
    }
}
