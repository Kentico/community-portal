using CMS.ContentEngine;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.Blog.Events;

/// <summary>
/// Handles automatically creating a new Q and A Question
/// used for discussion of a blog post when a <see cref="BlogPostPage"/> is published
/// </summary>
public class BlogPostPublishCreateQAndAQuestionHandler
{
    private readonly IHttpContextAccessor accessor;
    private readonly IMediator mediator;
    private readonly IContentQueryExecutor executor;
    private readonly IWebPageQueryResultMapper mapper;
    private readonly IWebPageUrlRetriever pageUrlRetriever;

    public BlogPostPublishCreateQAndAQuestionHandler(
        IHttpContextAccessor accessor,
        IMediator mediator,
        IContentQueryExecutor executor,
        IWebPageQueryResultMapper mapper,
        IWebPageUrlRetriever pageUrlRetriever
    )
    {
        this.accessor = accessor;
        this.mediator = mediator;
        this.executor = executor;
        this.mapper = mapper;
        this.pageUrlRetriever = pageUrlRetriever;
    }

    public async Task Handle(PublishWebPageEventArgs args)
    {
        /*
         * Only perform search indexing when a request is available (eg not during CI restore)
         */
        if (accessor.HttpContext is null)
        {
            return;
        }

        /*
         * We don't use the pre-built queries for this page and the root page below
         * because they are heavily dependent on the IWebsiteChannelContext which
         * doesn't work in an environment where the Admin runs under a different
         * domain from the website channel - as is the case with most multi-website
         * channel solutions
         * 
         * TODO - consider using a wrapper that can fallback to a custom context
         * populated manually before queries are executed 🤔
         */
        var page = await GetBlogPostPage(args);

        if (!string.IsNullOrEmpty(page.BlogPostPageQAndADiscussionLinkPath))
        {
            return;
        }

        var rootQuestionPage = await GetRootPage(args);

        var url = await pageUrlRetriever.Retrieve(page);
        string questionTitle = $"Blog Discussion: {page.BlogPostPageTitle}";
        string questionContent = $"""
            Blog Post: [{page.BlogPostPageTitle}]({url.RelativePathTrimmed()})

            Continue discussions 🤗 on this blog post below.
            """;
        var member = new CommunityMember()
        {
            Id = 0 // Only the Id is required and an Id of 0 will result in the author being the Kentico Community author
        };
        var command = new QAndAQuestionCreateCommand(member, rootQuestionPage, args.WebsiteChannelID, questionTitle, questionContent);
        int questionWebPageID = await mediator.Send(command, default);
        var questionPageURL = await pageUrlRetriever.Retrieve(questionWebPageID, args.ContentLanguageName);

        /*
         * This will recurse on this handler because it publishes the blog post
         * but we guard against updating a blog post page that already has a question path
         */
        _ = await mediator.Send(new BlogPostPageUpdateCommand(page, args.WebsiteChannelID, questionPageURL));
    }

    private async Task<BlogPostPage> GetBlogPostPage(PublishWebPageEventArgs args)
    {
        var routedPage = new RoutedWebPage
        {
            ContentTypeName = args.ContentTypeName,
            LanguageName = args.ContentLanguageName,
            WebPageItemID = args.ID
        };

        var b = new ContentItemQueryBuilder().ForWebPage(args.WebsiteChannelName, routedPage);
        var pages = await executor.GetWebPageResult(b, mapper.Map<BlogPostPage>);

        return pages.First();
    }

    private async Task<QAndAQuestionsRootPage> GetRootPage(PublishWebPageEventArgs args)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(QAndAQuestionsRootPage.CONTENT_TYPE_NAME, parameters => parameters.ForWebsite(args.WebsiteChannelName).TopN(1));

        var pages = await executor.GetWebPageResult(b, mapper.Map<QAndAQuestionsRootPage>);

        return pages.First();
    }
}
