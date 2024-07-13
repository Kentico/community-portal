using CMS.Core;
using CMS.Membership;
using CMS.Websites.Routing;
using Htmx;
using Kentico.Community.Portal.Core;
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
    ISystemClock clock,
    IEventLogService log) : PortalHandlerController(log)
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IUserInfoProvider userInfoProvider = userInfoProvider;
    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly ISystemClock clock = clock;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(QAndAQuestionFormSubmissionViewModel requestModel)
    {
        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(QAndAQuestionFormViewComponent));
        }

        var member = await userManager.CurrentUser(HttpContext);

        if (member is null)
        {
            return Unauthorized();
        }

        var now = clock.Now;
        var questionsParent = await mediator.Send(new QAndAQuestionsRootPageQuery(channelContext.WebsiteChannelName));
        var questionMonthFolder = await mediator.Send(new QAndAMonthFolderQuery(questionsParent, channelContext.WebsiteChannelName, now.Year, now.Month, channelContext.WebsiteChannelID));

        return await mediator.Send(new QAndAQuestionCreateCommand(
            member,
            questionMonthFolder,
            channelContext.WebsiteChannelID,
            requestModel.Title,
            requestModel.Content,
            SystemTaxonomies.QAndADiscussionTypeTaxonomy.QuestionTag.GUID,
            Maybe<BlogPostPage>.None))
            .Match(_ =>
            {
                Response.Htmx(h => h.Redirect("/q-and-a"));

                return Ok();
            }, LogAndReturnError("CREATE_QUESTION"));
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

        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));
        if (question is null)
        {
            return NotFound();
        }

        return await mediator.Send(new QAndAQuestionUpdateCommand(question, requestModel.Title, requestModel.Content, channelContext.WebsiteChannelID))
            .Match<StatusCodeResult>(async () =>
            {
                string redirectURL = (await urlRetriever.Retrieve(question)).RelativePathTrimmed();

                Response.Htmx(h => h.Redirect(redirectURL));

                return Ok();
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

        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));
        if (question is null)
        {
            return NotFound();
        }

        bool canManageContent = await userManager.CanManageContent(member, userInfoProvider);

        if (question.QAndAQuestionPageAuthorMemberID != member.Id && !canManageContent)
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

        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));
        if (question is null)
        {
            return NotFound();
        }

        return await mediator.Send(new QAndAQuestionDeleteCommand(question, channelContext.WebsiteChannelID))
            .Match(Ok, LogAndReturnError("QUESTION_DELETE"));
    }
}
