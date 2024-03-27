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
    IWebsiteChannelContext channelContext) : Controller
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IUserInfoProvider userInfoProvider = userInfoProvider;
    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext channelContext = channelContext;

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

        var questionParent = await mediator.Send(new QAndAQuestionsRootPageQuery(channelContext.WebsiteChannelName));

        _ = await mediator.Send(new QAndAQuestionCreateCommand(
            member,
            questionParent,
            channelContext.WebsiteChannelID,
            requestModel.Title,
            requestModel.Content,
            SystemTaxonomies.QAndADiscussionTypeTaxonomy.QuestionTag.GUID));

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

        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));
        if (question is null)
        {
            return NotFound();
        }

        _ = await mediator.Send(new QAndAQuestionUpdateCommand(question, requestModel.Title, requestModel.Content, channelContext.WebsiteChannelID));

        string redirectURL = (await urlRetriever.Retrieve(question)).RelativePathTrimmed();

        Response.Htmx(h => h.Redirect(redirectURL));

        return Ok();
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

        _ = await mediator.Send(new QAndAQuestionDeleteCommand(question, channelContext.WebsiteChannelID));

        return Ok();
    }
}
