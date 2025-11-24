using CMS.Base;
using Htmx;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Admin.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kentico.Community.Portal.Web.Features.QAndA;

[Authorize]
[Route("[controller]/[action]")]
public class QAndAQuestionController(
    UserManager<CommunityMember> userManager,
    IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    TimeProvider clock,
    IContentRetriever contentRetriever,
    ILogger<QAndAQuestionController> logger,
    IQAndAPermissionService permissionService,
    IReadOnlyModeProvider readOnlyProvider) : PortalHandlerController<QAndAQuestionController>(logger)
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly TimeProvider clock = clock;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IQAndAPermissionService permissionService = permissionService;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(QAndARateLimitingConstants.CreateQuestion)]
    public async Task<IActionResult> CreateQuestion(QAndAQuestionFormSubmissionViewModel requestModel)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return ViewComponent(typeof(QAndAQuestionFormViewComponent), new { submission = requestModel });
        }

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAQuestionFormViewComponent), new { submission = requestModel });
        }

        var member = await userManager.CurrentUser(HttpContext);
        if (member is null)
        {
            return Unauthorized();
        }

        var now = clock.GetLocalNow();
        var rootPages = await contentRetriever.RetrievePages<QAndAQuestionsRootPage>();
        if (rootPages.FirstOrDefault() is not QAndAQuestionsRootPage rootPage)
        {
            return LogAndReturnError("QUESTION_CREATE")($"Could not find required page of type [{QAndAQuestionsRootPage.CONTENT_TYPE_NAME}]");
        }
        var questionMonthFolder = await mediator.Send(new QAndAMonthFolderQuery(rootPage, now.Year, now.Month));


        return await mediator.Send(new QAndAQuestionCreateCommand(
            member,
            questionMonthFolder,
            rootPage.SystemFields.WebPageItemWebsiteChannelId,
            requestModel.Title,
            requestModel.Content,
            SystemTaxonomies.QAndADiscussionTypeTaxonomy.QuestionTag.GUID,
            requestModel.SelectedTags,
            Maybe<BlogPostPage>.None))
            .Match(
                _ =>
                {
                    Response.Htmx(headers => headers.WithTrigger("cleanupEditor"));

                    return PartialView("~/Features/QAndA/Components/Form/QAndAQuestionFormConfirmation.cshtml") as IActionResult;
                },
                LogAndReturnError("QUESTION_CREATE"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(QAndARateLimitingConstants.UpdateQuestion)]
    public async Task<IActionResult> UpdateQuestion(QAndAQuestionFormSubmissionViewModel requestModel)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return ViewComponent(typeof(QAndAQuestionFormViewComponent), new { questionID = requestModel.EditedObjectID, submission = requestModel });
        }

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAQuestionFormViewComponent), new { questionID = requestModel.EditedObjectID, submission = requestModel });
        }

        if (requestModel.EditedObjectID is not Guid questionID)
        {
            return NotFound();
        }

        var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });
        if (questionPages.FirstOrDefault() is not QAndAQuestionPage questionPage)
        {
            return NotFound();
        }

        var member = await userManager.CurrentUser(HttpContext);
        bool canManageContent = await permissionService.HasPermission(HttpContext, questionPage, QAndAQuestionPermissionType.Edit);
        if (questionPage.QAndAQuestionPageAuthorMemberID != member!.Id && !canManageContent)
        {
            return Forbid();
        }

        return await mediator
            .Send(new QAndAQuestionUpdateCommand(questionPage, requestModel.Title, requestModel.Content, requestModel.SelectedTags))
            .Match(async () =>
            {
                string redirectURL = (await urlRetriever.Retrieve(questionPage)).RelativePathTrimmed();
                Response.Htmx(h => h.Redirect(redirectURL));

                return Ok().AsStatusCodeResult();
            }, LogAndReturnErrorAsync("QUESTION_UPDATE"));
    }

    [HttpGet]
    public async Task<IActionResult> DisplayEditQuestionForm(Guid questionID)
    {
        var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });
        if (questionPages.FirstOrDefault() is not QAndAQuestionPage questionPage)
        {
            return NotFound();
        }

        var member = await userManager.CurrentUser(HttpContext);
        bool canManageContent = await permissionService.HasPermission(HttpContext, questionPage, QAndAQuestionPermissionType.Edit);
        if (questionPage.QAndAQuestionPageAuthorMemberID != member!.Id && !canManageContent)
        {
            return Forbid();
        }

        return ViewComponent(typeof(QAndAQuestionFormViewComponent), new { questionID });
    }


    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> DeleteQuestion(Guid questionID)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });
        if (questionPages.FirstOrDefault() is not QAndAQuestionPage questionPage)
        {
            return NotFound();
        }

        bool canManageContent = await permissionService.HasPermission(HttpContext, questionPage, QAndAQuestionPermissionType.Delete);
        if (!canManageContent)
        {
            return Forbid();
        }

        var rootPages = await contentRetriever.RetrievePages<QAndAQuestionsRootPage>();
        if (rootPages.FirstOrDefault() is not QAndAQuestionsRootPage rootPage)
        {
            return LogAndReturnError("QUESTION_DELETE")($"Could not find required page of type [{QAndAQuestionsRootPage.CONTENT_TYPE_NAME}]");
        }

        return await mediator.Send(new QAndAQuestionDeleteCommand(questionPage))
            .Match(async () =>
            {
                string path = (await urlRetriever.Retrieve(rootPage)).RelativePathTrimmed();
                Response.Htmx(h => h.Redirect(path));

                return Ok().AsStatusCodeResult();
            },
            LogAndReturnErrorAsync("QUESTION_DELETE"));
    }
}
