using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;
using Kentico.Community.Portal.Web.Features.QAndA.Search;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    contentTypeName: QAndAQuestionPage.CONTENT_TYPE_NAME,
    controllerType: typeof(QAndAQuestionPageController),
    ActionName = nameof(QAndAQuestionPageController.Index)
)]

namespace Kentico.Community.Portal.Web.Features.QAndA;

[Route("[controller]/[action]")]
public class QAndAQuestionPageController(
    UserManager<CommunityMember> userManager,
    IQAndAPermissionService permissionService,
    WebPageMetaService metaService,
    IMediator mediator,
    MemberBadgeService memberBadgeService,
    IContentRetriever contentRetriever,
    MarkdownRenderer markdownRenderer,
    QAndANotificationSettingsManager notificationSettingsManager) : Controller
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IQAndAPermissionService permissionService = permissionService;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;
    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;
    private readonly MemberBadgeService memberBadgeService = memberBadgeService;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly QAndANotificationSettingsManager notificationSettingsManager = notificationSettingsManager;

    [HttpGet]
    public async Task<ActionResult> Index()
    {
        var currentMember = await userManager.CurrentUser(HttpContext);

        var question = await contentRetriever.RetrieveCurrentPage<QAndAQuestionPage>(new RetrieveCurrentPageParameters { LinkedItemsMaxLevel = 1 });
        metaService.SetMeta(new WebPageMeta(
            question.BasicItemTitle,
            $"View the discussion about {question.BasicItemTitle} in the Kentico Community Portal Q&A."));

        var answers = await GetAnswers(question, currentMember);
        var taxonomyResp = await mediator.Send(new QAndATaxonomiesQuery());
        var questionVM = await MapQuestion(question, currentMember, taxonomyResp);

        var vm = new QAndAQuestionPageViewModel(questionVM, answers);

        return View("~/Features/QAndA/QAndAQuestionPage.cshtml", vm);
    }

    [HttpGet]
    public async Task<ActionResult> DisplayQuestionDetail(Guid questionID)
    {
        var questionPage = await contentRetriever
            .RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 })
            .TryFirst();
        if (!questionPage.TryGetValue(out var question))
        {
            return NotFound();
        }

        var currentMember = await userManager.CurrentUser(HttpContext);
        var taxonomyResp = await mediator.Send(new QAndATaxonomiesQuery());
        var vm = await MapQuestion(question, currentMember, taxonomyResp);

        return PartialView("~/Features/QAndA/_QAndAQuestionDetail.cshtml", vm);
    }

    [HttpGet]
    public async Task<ActionResult> DisplayAnswerDetail(Guid questionID, int answerID)
    {
        var answer = await mediator.Send(new QAndAAnswerDataByIDQuery(answerID));
        if (answer is null)
        {
            return NotFound();
        }

        var questionPage = await contentRetriever
            .RetrievePagesByGuids<QAndAQuestionPage>([questionID], new RetrievePagesParameters { LinkedItemsMaxLevel = 1 })
            .TryFirst();
        if (!questionPage.TryGetValue(out var question))
        {
            return NotFound();
        }

        var currentMember = await userManager.CurrentUser(HttpContext);
        var vm = await MapAnswer(question, answer, currentMember, permissionService);

        return PartialView("~/Features/QAndA/_QAndAAnswerDetail.cshtml", vm);
    }

    [HttpGet]
    public ActionResult DisplayAnswerButton(Guid questionID) =>
        PartialView("~/Features/QAndA/Components/Form/_QAndAAnswerButton.cshtml", questionID);

    private async Task<IReadOnlyList<QAndAPostAnswerViewModel>> GetAnswers(
        QAndAQuestionPage question,
        CommunityMember? currentMember)
    {
        var res = await mediator.Send(new QAndAAnswerDatasByQuestionQuery(question.SystemFields.WebPageItemID, currentMember?.Id));
        var vms = new List<QAndAPostAnswerViewModel>();
        foreach (var answerData in res.Items)
        {
            var answerModel = await MapAnswer(question, answerData.Answer, currentMember, permissionService, answerData);
            vms.Add(answerModel);
        }

        return [.. vms.OrderBy(a => a.DateCreated)];
    }

    private async Task<QAndAPostQuestionViewModel> MapQuestion(
        QAndAQuestionPage question,
        CommunityMember? currentMember,
        QAndATaxonomiesQueryResponse taxonomiesResp)
    {
        var author = await GetAuthor(question.QAndAQuestionPageAuthorMemberID);
        var permissions = await permissionService.GetPermissions(currentMember, question);

        bool isSubscribed = false;
        if (currentMember is not null)
        {
            var webPageItemIDs = await notificationSettingsManager.GetSubscribedWebPageItemIDs(currentMember);
            isSubscribed = webPageItemIDs.Contains(question.SystemFields.WebPageItemID);
        }

        var model = new QAndAPostQuestionViewModel(question, permissions, currentMember, taxonomiesResp, author, markdownRenderer, isSubscribed);

        // Load reaction data (always show count, even for unauthenticated users)
        var reactionData = await mediator.Send(new QAndAQuestionReactionsQuery(question.SystemFields.WebPageItemID, currentMember?.Id));
        model.Reactions = new(question.SystemFields.WebPageItemGUID, reactionData, permissions);

        return model;
    }

    private async Task<QAndAPostAnswerViewModel> MapAnswer(
        QAndAQuestionPage question,
        QAndAAnswerDataInfo answer,
        CommunityMember? currentMember,
        IQAndAPermissionService permissionService,
        AnswerWithReactionsData? reactionData = null)
    {
        var permissions = await permissionService.GetPermissions(currentMember, question, answer);

        var author = await GetAuthor(answer.QAndAAnswerDataAuthorMemberID);
        var reactionQueryResult = reactionData is null
            ? await mediator.Send(new QAndAAnswerReactionsQuery(answer.QAndAAnswerDataID, currentMember?.Id))
            : reactionData.Reactions;

        var model = new QAndAPostAnswerViewModel
        {
            ID = answer.QAndAAnswerDataID,
            GUID = answer.QAndAAnswerDataGUID,
            ParentQuestionID = question.SystemFields.WebPageItemGUID,
            HTMLSanitizedContentHTML = markdownRenderer.Render(answer.QAndAAnswerDataContent),
            Author = author,
            DateCreated = answer.QAndAAnswerDataDateCreated,
            DateModified = answer.QAndAAnswerDataDateModified,
            IsAcceptedAnswer = answer.QAndAAnswerDataGUID == question.QAndAQuestionPageAcceptedAnswerDataGUID,
            Permissions = permissions,
            Reactions = new(answer.QAndAAnswerDataID, reactionQueryResult, permissions, question.SystemFields.WebPageItemGUID)
        };

        return model;
    }

    private async Task<QAndAAuthorViewModel> GetAuthor(int memberID)
    {
        var member = await userManager.FindByIdAsync(memberID.ToString());
        if (member is not null)
        {
            return new(member)
            {
                SelectedBadges = await memberBadgeService.GetSelectedBadgesFor(memberID)
            };
        }


        var resp = await mediator.Send(new AuthorContentQuery(AuthorContent.KENTICO_AUTHOR_CODE_NAME));
        if (resp.Author is null)
        {
            throw new Exception($"Missing Author [{AuthorContent.KENTICO_AUTHOR_CODE_NAME}] which is required to display Q&A");
        }

        var model = new QAndAAuthorViewModel(resp.Author)
        {
            SelectedBadges = await memberBadgeService.GetSelectedBadgesFor(memberID)
        };

        return model;
    }
}

public class QAndAQuestionPageViewModel
{
    public QAndAPostQuestionViewModel Question { get; }
    public IReadOnlyList<QAndAPostAnswerViewModel> Answers { get; }
    public bool HasAcceptedAnswer { get; }

    public QAndAQuestionPageViewModel(
        QAndAPostQuestionViewModel question,
        IReadOnlyList<QAndAPostAnswerViewModel> answers)
    {
        Question = question;
        Answers = answers;
        HasAcceptedAnswer = answers.Any(a => a.IsAcceptedAnswer);
    }
}

public class QAndAPostQuestionViewModel
{
    public Guid ID { get; set; } = Guid.Empty;
    public string Title { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public QAndAAuthorViewModel Author { get; set; } = new();
    public HtmlSanitizedHtmlString HTMLSanitizedContentHTML { get; set; } = HtmlSanitizedHtmlString.Empty;
    public QAndAQuestionPermissions Permissions { get; set; } = QAndAQuestionPermissions.NoPermissions;
    public IReadOnlyList<string> DXTopics { get; } = [];
    public bool IsSubscribed { get; set; }
    public QAndAQuestionReactionViewModel Reactions { get; set; } = QAndAQuestionReactionViewModel.Empty;

    public QAndAPostQuestionViewModel(
        QAndAQuestionPage question,
        QAndAQuestionPermissions permissions,
        CommunityMember? currentMember,
        QAndATaxonomiesQueryResponse taxonomiesResp,
        QAndAAuthorViewModel author,
        MarkdownRenderer markdownRenderer,
        bool isSubscribed
    )
    {
        bool ownsQuestion = currentMember?.Id == question.QAndAQuestionPageAuthorMemberID;

        ID = question.SystemFields.WebPageItemGUID;
        Title = question.BasicItemTitle;
        HTMLSanitizedContentHTML = markdownRenderer.Render(question.QAndAQuestionPageContent);
        DateCreated = question.QAndAQuestionPageDateCreated;
        DateModified = question.QAndAQuestionPageDateModified;
        Author = author;
        Permissions = permissions;
        IsSubscribed = isSubscribed;
        var tagRefs = question.CoreTaxonomyDXTopics.Select(t => t.Identifier);
        DXTopics = [.. taxonomiesResp
            .DXTopicsAll
            .Where(t => tagRefs.Contains(t.Guid))
            .Select(t => t.DisplayName)];
    }
}

public class QAndAPostAnswerViewModel
{
    public int ID { get; set; }
    public Guid GUID { get; set; }
    public Guid ParentQuestionID { get; set; }
    public QAndAAuthorViewModel Author { get; set; } = new();
    public HtmlSanitizedHtmlString HTMLSanitizedContentHTML { get; set; } = HtmlSanitizedHtmlString.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public bool IsAcceptedAnswer { get; set; }
    public QAndAAnswerPermissions Permissions { get; set; } = QAndAAnswerPermissions.NoPermissions;
    public QAndADiscussionReactionViewModel Reactions { get; set; } = QAndADiscussionReactionViewModel.Empty;
}

public class QAndADiscussionReactionViewModel
{
    public int AnswerID { get; }
    public Guid ParentQuestionID { get; }
    public bool CurrentMemberHasReacted { get; }
    public int ReactionCount { get; }
    public IReadOnlyList<string> MembersWhoReacted { get; } = [];
    public QAndAAnswerPermissions Permissions { get; set; } = QAndAAnswerPermissions.NoPermissions;

    public static QAndADiscussionReactionViewModel Empty { get; } = new();

    public QAndADiscussionReactionViewModel(int answerID, AnswerReactionsData data, QAndAAnswerPermissions permissions, Guid parentQuestionID)
    {
        AnswerID = answerID;
        ParentQuestionID = parentQuestionID;
        CurrentMemberHasReacted = data.CurrentMemberHasReacted;
        ReactionCount = data.TotalCount;
        MembersWhoReacted = data.MembersWhoReacted;
        Permissions = permissions;
    }

    private QAndADiscussionReactionViewModel() { }
}

public class QAndAQuestionReactionViewModel
{
    public Guid QuestionID { get; }
    public bool CurrentMemberHasReacted { get; }
    public int ReactionCount { get; }
    public IReadOnlyList<string> MembersWhoReacted { get; } = [];
    public QAndAQuestionPermissions Permissions { get; set; } = QAndAQuestionPermissions.NoPermissions;

    public static QAndAQuestionReactionViewModel Empty { get; } = new();

    public QAndAQuestionReactionViewModel(Guid questionID, QuestionReactionsData data, QAndAQuestionPermissions permissions)
    {
        QuestionID = questionID;
        CurrentMemberHasReacted = data.CurrentMemberHasReacted;
        ReactionCount = data.TotalCount;
        MembersWhoReacted = data.MembersWhoReacted;
        Permissions = permissions;
    }

    private QAndAQuestionReactionViewModel() { }
}

public class QAndAAuthorViewModel
{
    public int ID { get; }
    public string Username { get; } = "";
    public string FullName { get; } = "";
    public string FormattedName =>
        string.IsNullOrWhiteSpace(FullName)
            ? Username
            : FullName;

    public IReadOnlyList<MemberBadgeViewModel> SelectedBadges { get; set; } = [];

    public DiscussionAuthorAttributes AuthorAttributes { get; set; } = DiscussionAuthorAttributes.Default;

    public QAndAAuthorViewModel(CommunityMember member)
    {
        ID = member.Id;
        Username = member.UserName!;
        FullName = member.FullName;
        AuthorAttributes = new();
    }

    public QAndAAuthorViewModel(AuthorContent author)
    {
        ID = 0;
        FullName = author.FullName;
        Username = author.AuthorContentCodeName;
    }

    public QAndAAuthorViewModel(int memberID, string fullName, string username, DiscussionAuthorAttributes? authorAttributes)
    {
        ID = memberID;
        FullName = fullName;
        Username = username;
        AuthorAttributes = authorAttributes ?? DiscussionAuthorAttributes.Default;
    }
    public QAndAAuthorViewModel() { }
}

