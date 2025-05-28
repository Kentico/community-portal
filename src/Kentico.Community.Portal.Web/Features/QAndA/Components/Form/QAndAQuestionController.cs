using CMS.Core;
using CMS.Membership;
using CMS.Websites.Routing;
using Htmx;
using Kentico.Community.Portal.Core.Modules;
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
public class QAndAQuestionController(
    UserManager<CommunityMember> userManager,
    IUserInfoProvider userInfoProvider,
    IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext channelContext,
    TimeProvider clock,
    IEventLogService log) : PortalHandlerController(log)
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IUserInfoProvider userInfoProvider = userInfoProvider;
    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly TimeProvider clock = clock;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(QAndAQuestionFormSubmissionViewModel requestModel)
    {
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
        var questionsParent = await mediator.Send(new QAndAQuestionsRootPageQuery(channelContext.WebsiteChannelName));
        var questionMonthFolder = await mediator.Send(new QAndAMonthFolderQuery(questionsParent, channelContext.WebsiteChannelName, now.Year, now.Month, channelContext.WebsiteChannelID));


        return await mediator.Send(new QAndAQuestionCreateCommand(
            member,
            questionMonthFolder,
            channelContext.WebsiteChannelID,
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
                LogAndReturnError("CREATE_QUESTION"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuestion(QAndAQuestionFormSubmissionViewModel requestModel)
    {
        var member = await userManager.CurrentUser(HttpContext);
        if (member is null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAQuestionFormViewComponent), new { questionID = requestModel.EditedObjectID, submission = requestModel });
        }

        if (requestModel.EditedObjectID is not Guid questionID)
        {
            return NotFound();
        }

        var questionResp = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (!questionResp.TryGetValue(out var questionPage))
        {
            return NotFound();
        }

        bool canManageContent = await userManager.CanManageContent(member, userInfoProvider);
        if (questionPage.QAndAQuestionPageAuthorMemberID != member.Id && !canManageContent)
        {
            return Forbid();
        }

        return await mediator
            .Send(new QAndAQuestionUpdateCommand(questionPage, requestModel.Title, requestModel.Content, requestModel.SelectedTags, channelContext.WebsiteChannelID))
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
        var member = await userManager.CurrentUser(HttpContext);
        if (member is null)
        {
            return Unauthorized();
        }

        var questionResp = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (!questionResp.TryGetValue(out var questionPage))
        {
            return NotFound();
        }

        bool canManageContent = await userManager.CanManageContent(member, userInfoProvider);
        if (questionPage.QAndAQuestionPageAuthorMemberID != member.Id && !canManageContent)
        {
            return Forbid();
        }

        return ViewComponent(typeof(QAndAQuestionFormViewComponent), new { questionID });
    }


    [ValidateAntiForgeryToken]
    [HttpPost]
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

        var questionResp = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID));
        if (!questionResp.TryGetValue(out var questionPage))
        {
            return NotFound();
        }

        var questionsParent = await mediator.Send(new QAndAQuestionsRootPageQuery(channelContext.WebsiteChannelName));
        return await mediator.Send(new QAndAQuestionDeleteCommand(questionPage, channelContext.WebsiteChannelID))
            .Match(async () =>
            {
                string path = (await urlRetriever.Retrieve(questionsParent)).RelativePathTrimmed();
                Response.Htmx(h => h.Redirect(path));

                return Ok().AsStatusCodeResult();
            },
            LogAndReturnErrorAsync("QUESTION_DELETE"));
    }
}
