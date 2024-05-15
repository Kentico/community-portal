using CMS.Membership;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.QAndA;
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
    IWebPageDataContextRetriever dataContextRetriever,
    UserManager<CommunityMember> userManager,
    IUserInfoProvider userInfoProvider,
    WebPageMetaService metaService,
    IMediator mediator,
    MarkdownRenderer markdownRenderer,
    IWebsiteChannelContext channelContext) : Controller
{
    private readonly IWebPageDataContextRetriever dataContextRetriever = dataContextRetriever;
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IUserInfoProvider userInfoProvider = userInfoProvider;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;
    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    [HttpGet]
    public async Task<ActionResult> Index()
    {
        if (!dataContextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var currentMember = await userManager.CurrentUser(HttpContext);

        var question = await mediator.Send(new QAndAQuestionPageQuery(data.WebPage));
        metaService.SetMeta(new(question.QAndAQuestionPageTitle, $"View the discussion about {question.QAndAQuestionPageTitle} in the Kentico Community Portal Q&A."));

        bool canManageContent = await userManager.CanManageContent(currentMember, userInfoProvider);
        var answers = await GetAnswers(question, currentMember, canManageContent);

        var vm = new QAndAQuestionPageViewModel
        {
            Question = await MapQuestion(question, currentMember, canManageContent),
            Answers = answers
        };

        return View("~/Features/QAndA/QAndAQuestionPage.cshtml", vm);
    }

    [HttpGet]
    public async Task<ActionResult> DisplayQuestionDetail(Guid questionID)
    {
        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));
        if (question is null)
        {
            return NotFound();
        }

        var currentMember = await userManager.CurrentUser(HttpContext);
        bool canManageContent = await userManager.CanManageContent(currentMember, userInfoProvider);

        var vm = await MapQuestion(question, currentMember, canManageContent);

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

        var currentMember = await userManager.CurrentUser(HttpContext);
        bool canManageContent = await userManager.CanManageContent(currentMember, userInfoProvider);
        var question = await mediator.Send(new QAndAQuestionPageByGUIDQuery(questionID, channelContext.WebsiteChannelName));

        var vm = await MapAnswer(question, answer, currentMember, canManageContent);

        return PartialView("~/Features/QAndA/_QAndAAnswerDetail.cshtml", vm);
    }

    private async Task<IReadOnlyList<QAndAPostAnswerViewModel>> GetAnswers(
        QAndAQuestionPage question,
        CommunityMember? currentMember,
        bool canManageContent)
    {
        var res = await mediator.Send(new QAndAAnswerDatasByQuestionQuery(question.SystemFields.WebPageItemID));
        var vms = new List<QAndAPostAnswerViewModel>();
        foreach (var answer in res.Items)
        {
            var answerModel = await MapAnswer(question, answer, currentMember, canManageContent);
            vms.Add(answerModel);
        }

        return vms
            .OrderByDescending(a => a.IsAcceptedAnswer)
            .ThenBy(a => a.DateCreated)
            .ToList();
    }

    private async Task<QAndAPostQuestionViewModel> MapQuestion(
        QAndAQuestionPage question,
        CommunityMember? currentMember,
        bool canManageContent)
    {
        bool ownsQuestion = currentMember?.Id == question.QAndAQuestionPageAuthorMemberID;
        var author = await GetAuthor(question.QAndAQuestionPageAuthorMemberID);

        return new()
        {
            ID = question.SystemFields.WebPageItemGUID,
            Title = question.QAndAQuestionPageTitle,
            HTMLSanitizedContentHTML = markdownRenderer.Render(question.QAndAQuestionPageContent),
            DateCreated = question.QAndAQuestionPageDateCreated,
            DateModified = question.QAndAQuestionPageDateModified,
            Author = author,
            Permissions = new()
            {
                CanDelete = canManageContent,
                CanEdit = canManageContent || ownsQuestion,
                CanAnswer = currentMember is not null
            },
        };
    }

    private async Task<QAndAPostAnswerViewModel> MapAnswer(QAndAQuestionPage question, QAndAAnswerDataInfo answer, CommunityMember? currentMember, bool canManageContent)
    {
        var permissions = new QAndAManagementPermissions()
        {
            CanDelete = canManageContent,
            CanEdit = question.QAndAQuestionPageAcceptedAnswerDataGUID != answer.QAndAAnswerDataGUID
                && (canManageContent || currentMember?.Id == answer.QAndAAnswerDataAuthorMemberID),
            CanMarkAnswered = (canManageContent || currentMember?.Id == question.QAndAQuestionPageAuthorMemberID)
                && question.QAndAQuestionPageAcceptedAnswerDataGUID == default,
            CanAnswer = currentMember is not null
        };

        var author = await GetAuthor(answer.QAndAAnswerDataAuthorMemberID);

        return new QAndAPostAnswerViewModel
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
        };
    }

    private async Task<QAndAAuthorViewModel> GetAuthor(int memberID)
    {
        var member = await userManager.FindByIdAsync(memberID.ToString());
        if (member is not null)
        {
            return new(member);
        }

        var resp = await mediator.Send(new AuthorContentQuery(AuthorContent.KENTICO_AUTHOR_CODE_NAME));
        if (resp.Author is null)
        {
            throw new Exception($"Missing Author [{AuthorContent.KENTICO_AUTHOR_CODE_NAME}] which is required to display Q&A");
        }

        return new(resp.Author);
    }
}

public class QAndAQuestionPageViewModel
{
    public QAndAPostQuestionViewModel Question { get; set; } = new();
    public IReadOnlyList<QAndAPostAnswerViewModel> Answers { get; set; } = [];
}

public class QAndAPostQuestionViewModel
{
    public Guid ID { get; set; } = Guid.Empty;
    public string Title { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public QAndAAuthorViewModel Author { get; set; } = new();
    public HtmlSanitizedHtmlString HTMLSanitizedContentHTML { get; set; } = HtmlSanitizedHtmlString.Empty;
    public QAndAManagementPermissions Permissions { get; set; } = new();
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
    public QAndAManagementPermissions Permissions { get; set; } = new();
}

public class QAndAAuthorViewModel
{
    public int ID { get; }
    public string Username { get; } = "";
    public string FullName { get; } = "";
    public string FormattedName =>
        string.IsNullOrWhiteSpace(FullName)
            ? Username
            : $"{FullName} ({Username})";

    public DiscussionAuthorAttributes AuthorAttributes { get; set; } = DiscussionAuthorAttributes.Default;

    public QAndAAuthorViewModel(CommunityMember member)
    {
        ID = member.Id;
        Username = member.UserName!;
        FullName = member.FullName;
        AuthorAttributes = new()
        {
            IsMVP = member.IsMVP
        };
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

public class QAndAManagementPermissions
{
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanMarkAnswered { get; set; }
    public bool CanAnswer { get; set; }

    public bool CanInteract =>
        CanEdit || CanDelete || CanMarkAnswered || CanAnswer;
}

