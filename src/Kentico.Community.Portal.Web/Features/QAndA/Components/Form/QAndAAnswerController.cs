using CMS.Base;
using Htmx;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kentico.Community.Portal.Web.Features.QAndA;

[Authorize]
[Route("[controller]/[action]")]
public class QAndAAnswerController(
    UserManager<CommunityMember> userManager,
    IWebPageUrlRetriever urlRetriever,
    IMediator mediator,
    IContentRetriever contentRetriever,
    IQAndAPermissionService permissionService,
    ILogger<QAndAAnswerController> logger,
    IReadOnlyModeProvider readOnlyProvider) : PortalHandlerController<QAndAAnswerController>(logger)
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IMediator mediator = mediator;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IQAndAPermissionService permissionService = permissionService;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(QAndARateLimitingConstants.CreateAnswer)]
    public async Task<ActionResult> CreateAnswer(QAndAAnsweredViewModel requestModel)
    {
        var questionID = requestModel.ParentQuestionID;

        if (readOnlyProvider.IsReadOnly)
        {
            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID });
        }

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID });
        }

        var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>([questionID]);
        if (questionPages.FirstOrDefault() is not QAndAQuestionPage parentQuestionPage)
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
            .Send(new QAndAAnswerCreateCommand(member, requestModel.Content, parentQuestionPage))
            .Match(async id =>
            {
                string questionPath = (await urlRetriever.Retrieve(parentQuestionPage)).RelativePathTrimmed();
                Response.Htmx(h => h.Redirect(questionPath));

                return Ok().AsStatusCodeResult();

            }, LogAndReturnErrorAsync("ANSWER_CREATE"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(QAndARateLimitingConstants.UpdateAnswer)]
    public async Task<ActionResult> UpdateAnswer(QAndAAnsweredViewModel requestModel)
    {
        var questionID = requestModel.ParentQuestionID;

        if (readOnlyProvider.IsReadOnly)
        {
            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID, answerID = requestModel.EditedObjectID });
        }

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID, answerID = requestModel.EditedObjectID });
        }

        if (requestModel.EditedObjectID is not int answerID)
        {
            return NotFound();
        }

        var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });
        if (questionPages.FirstOrDefault() is not QAndAQuestionPage parentQuestionPage)
        {
            ModelState.AddModelError(nameof(questionID), "Question is not valid");

            return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID, answerID });
        }

        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));
        if (answer is null)
        {
            return NotFound();
        }

        bool hasPermission = await permissionService.HasPermission(HttpContext, parentQuestionPage, answer, QAndAAnswerPermissionType.Edit);
        if (!hasPermission)
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
        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));
        if (answer is null)
        {
            return NotFound();
        }

        var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });
        if (questionPages.FirstOrDefault() is not QAndAQuestionPage parentQuestionPage)
        {
            ModelState.AddModelError(nameof(questionID), "Question is not valid");

            return NotFound();
        }

        bool hasPermission = await permissionService.HasPermission(HttpContext, parentQuestionPage, answer, QAndAAnswerPermissionType.Edit);
        if (!hasPermission)
        {
            return Forbid();
        }

        return ViewComponent(typeof(QAndAAnswerFormViewComponent), new { questionID, answerID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> MarkApprovedAnswer(Guid questionID, int answerID)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });
        if (questionPages.FirstOrDefault() is not QAndAQuestionPage parentQuestionPage)
        {
            return NotFound();
        }

        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));
        if (answer is null)
        {
            return NotFound();
        }

        bool hasPermission = await permissionService.HasPermission(HttpContext, parentQuestionPage, answer, QAndAAnswerPermissionType.MarkApprovedAnswer);
        if (!hasPermission)
        {
            return Forbid();
        }

        return await mediator
            .Send(new QAndAQuestionMarkAnsweredCommand(parentQuestionPage, answer))
            .Match(async () =>
            {
                string questionPath = (await urlRetriever.Retrieve(parentQuestionPage)).RelativePathTrimmed();

                Response.Htmx(h => h.Redirect(questionPath));

                return Ok().AsStatusCodeResult();
            }, LogAndReturnErrorAsync("ANSWER_APPROVED"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteAnswer(Guid questionID, int answerID)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));
        if (answer is null)
        {
            return NotFound();
        }

        var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });
        if (questionPages.FirstOrDefault() is not QAndAQuestionPage parentQuestionPage)
        {
            return NotFound();
        }

        bool hasPermission = await permissionService.HasPermission(HttpContext, parentQuestionPage, answer, QAndAAnswerPermissionType.Delete);
        if (!hasPermission)
        {
            return Forbid();
        }

        // If this answer is currently selected as the accepted answer, clear it first, then delete
        return await Result.Success()
            .BindIf(
                parentQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID == answer.QAndAAnswerDataGUID,
                () => mediator.Send(new QAndAQuestionClearAnsweredCommand(parentQuestionPage)))
            .Bind(() => mediator.Send(new QAndAAnswerDeleteCommand(answer)))
            .Match(() =>
            {
                Response.Htmx(h => h.WithToastSuccess("Answer successfully deleted."));
                return Ok().AsStatusCodeResult();
            }, LogAndReturnError("ANSWER_DELETE"));
    }
}
