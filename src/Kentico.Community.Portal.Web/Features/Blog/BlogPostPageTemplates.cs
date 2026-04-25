using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.BlogPostPage_Components",
    name: "Blog Post Page - Components",
    propertiesType: typeof(BlogPostPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogPostPage_Components.cshtml",
    ContentTypeNames = [BlogPostPage.CONTENT_TYPE_NAME],
    Description = "Requires the blog post page to be built with Page Builder components",
    IconClass = Icons.LListArticle
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: BlogPostPage.CONTENT_TYPE_NAME,
    controllerType: typeof(BlogPostPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogPostPageTemplateProperties : IPageTemplateProperties
{
    [CheckBoxComponent(
        Label = "Show table of contents",
        ExplanationText = "If checked, a floating table of contents panel is displayed for readers to navigate between sections.",
        Order = 1
    )]
    public bool ShowTableOfContents { get; set; } = true;
}

public class BlogPostPageTemplateController(
    WebPageMetaService metaService,
    IMediator mediator,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever urlRetriever,
    UserManager<CommunityMember> userManager) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly UserManager<CommunityMember> userManager = userManager;

    public async Task<ActionResult> Index()
    {
        var blogPage = await contentRetriever.RetrieveCurrentPage<BlogPostPage>(new RetrieveCurrentPageParameters() { LinkedItemsMaxLevel = BlogPostPage.FullQueryDepth });
        if (blogPage is null)
        {
            return NotFound();
        }

        var author = await GetAuthor(blogPage);
        string absoluteURL = Request.Path.Value!.RelativePathToAbsoluteURL(Request);
        var discussionURL = await blogPage.BlogPostPageQAndAQuestionPages
            .TryFirst()
            .Map(p => urlRetriever.Retrieve(p))
            .Map(u => u.RelativePath);
        int commentCount = await GetDiscussionCommentCount(blogPage);
        var upvote = await GetBlogPostUpvote(blogPage);
        var vm = new BlogPostPageViewModel(blogPage, author, discussionURL, absoluteURL, commentCount, upvote);

        metaService.SetMeta(blogPage);

        return new TemplateResult(vm);
    }

    private async Task<AuthorViewModel> GetAuthor(BlogPostPage post)
    {
        var author = post.BlogPostPageAuthorContent.FirstOrDefault();
        if (author is not null)
        {
            return new(author, Url);
        }
        var authors = await contentRetriever.RetrieveContent<AuthorContent>(
            RetrieveContentParameters.Default,
            p => p.Where(w => w.WhereEquals(nameof(AuthorContent.AuthorContentCodeName), AuthorContent.KENTICO_AUTHOR_CODE_NAME)),
            new RetrievalCacheSettings($"{nameof(AuthorContent.AuthorContentCodeName)}|{AuthorContent.KENTICO_AUTHOR_CODE_NAME}"));

        if (authors.FirstOrDefault() is not AuthorContent kenticoAuthor)
        {
            throw new Exception($"Missing Author [{AuthorContent.KENTICO_AUTHOR_CODE_NAME}] which is required to display BlogPosts");
        }

        return new(kenticoAuthor, Url);
    }

    private async Task<int> GetDiscussionCommentCount(BlogPostPage blogPostPage)
    {
        if (blogPostPage.BlogPostPageQAndAQuestionPages.FirstOrDefault() is not QAndAQuestionPage question)
        {
            return 0;
        }

        return await mediator.Send(new QAndAAnswerCountQuery(question.SystemFields.WebPageItemGUID));
    }

    private async Task<Maybe<BlogPostUpvoteViewModel>> GetBlogPostUpvote(BlogPostPage blogPostPage)
    {
        if (blogPostPage.BlogPostPageQAndAQuestionPages.FirstOrDefault() is not QAndAQuestionPage question)
        {
            return Maybe<BlogPostUpvoteViewModel>.None;
        }

        var currentMember = await userManager.CurrentUser(HttpContext);
        var reactionData = await mediator.Send(new QAndAQuestionReactionsQuery(question.SystemFields.WebPageItemID, currentMember?.Id));
        bool canReact = currentMember is not null && question.QAndAQuestionPageAuthorMemberID != currentMember.Id;

        return new BlogPostUpvoteViewModel(question.SystemFields.WebPageItemGUID, reactionData, canReact);
    }
}

public class BlogPostPageViewModel
{
    public string Title { get; } = "";
    public DateTime PublishedDate { get; }
    public AuthorViewModel Author { get; }
    public string AbsoluteURL { get; } = "";
    public Maybe<string> DiscussionLinkPath { get; }
    public int DiscussionCommentsCount { get; }
    public Maybe<BlogPostUpvoteViewModel> Upvote { get; }

    public BlogPostPageViewModel(BlogPostPage page, AuthorViewModel author, Maybe<string> discussionURL, string absoluteURL, int commentCount, Maybe<BlogPostUpvoteViewModel> upvote)
    {
        Title = page.BasicItemTitle;
        PublishedDate = page.BlogPostPagePublishedDate;
        Author = author;
        DiscussionLinkPath = discussionURL;
        AbsoluteURL = absoluteURL;
        DiscussionCommentsCount = commentCount;
        Upvote = upvote;
    }
}

public class AuthorViewModel
{
    public string Name { get; init; } = "";
    public Maybe<string> Title { get; init; }
    public Maybe<ImageViewModel> Photo { get; init; }
    public Maybe<string> LinkProfilePath { get; init; }
    public HtmlString BiographyHTML { get; init; } = HtmlString.Empty;

    public AuthorViewModel(AuthorContent author, IUrlHelper urlHelper)
    {
        Name = author.FullName;
        Photo = author.AuthorContentPhotoImageContent
            .TryFirst()
            .Map(ImageViewModel.Create);
        LinkProfilePath = author.AuthorContentMemberID == 0
            ? Maybe<string>.None
            : urlHelper.Action(nameof(MemberController.MemberDetail), "Member", new { memberID = author.AuthorContentMemberID });
        BiographyHTML = new(author.AuthorContentBiographyHTML);
    }
}

public class BlogPostUpvoteViewModel
{
    public Guid QuestionID { get; }
    public bool CurrentMemberHasReacted { get; }
    public int ReactionCount { get; }
    public IReadOnlyList<string> MembersWhoReacted { get; } = [];
    public bool CanReact { get; }

    public BlogPostUpvoteViewModel(Guid questionID, QuestionReactionsData data, bool canReact)
    {
        QuestionID = questionID;
        CurrentMemberHasReacted = data.CurrentMemberHasReacted;
        ReactionCount = data.TotalCount;
        MembersWhoReacted = data.MembersWhoReacted;
        CanReact = canReact;
    }
}
