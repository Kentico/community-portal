using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.Blog.Events;

/// <summary>
/// Handles automatically creating a new Q and A Question
/// used for discussion of a blog post when a <see cref="BlogPostPage"/> is published
/// </summary>
public class BlogPostPublishCreateQAndAQuestionHandler(
    IHttpContextAccessor accessor,
    IMediator mediator,
    IWebPageUrlRetriever pageUrlRetriever)
{
    private readonly IHttpContextAccessor accessor = accessor;
    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever pageUrlRetriever = pageUrlRetriever;

    public async Task Handle(PublishWebPageEventArgs args)
    {
        /*
         * Only perform search indexing when a request is available (eg not during CI restore)
         */
        if (accessor.HttpContext is null)
        {
            return;
        }

        var page = await mediator.Send(new BlogPostPageQuery(new RoutedWebPage
        {
            ContentTypeName = args.ContentTypeName,
            LanguageName = args.ContentLanguageName,
            WebPageItemID = args.ID,
            WebsiteChannelName = args.WebsiteChannelName
        }));

        if (!string.IsNullOrEmpty(page.BlogPostPageQAndADiscussionLinkPath))
        {
            return;
        }

        var rootQuestionPage = await mediator.Send(new QAndAQuestionsRootPageQuery(args.WebsiteChannelName));

        var url = await pageUrlRetriever.Retrieve(page);
        string postTitle = page.BlogPostPageBlogPostContent.FirstOrDefault()?.BlogPostContentTitle ?? "";
        string questionTitle = $"Blog Discussion: {postTitle}";
        string questionContent = $"""
            Blog Post: [{postTitle}]({url.RelativePathTrimmed()})

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
}
