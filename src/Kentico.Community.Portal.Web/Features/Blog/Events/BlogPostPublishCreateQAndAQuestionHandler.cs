using CMS.Core;
using Kentico.Community.Portal.Core.Modules;
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
    IMediator mediator,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever pageUrlRetriever,
    TimeProvider clock,
    IEventLogService log)
{
    private readonly IMediator mediator = mediator;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IWebPageUrlRetriever pageUrlRetriever = pageUrlRetriever;
    private readonly TimeProvider clock = clock;
    private readonly IEventLogService log = log;

    public async Task Handle(PublishWebPageEventArgs args)
    {
        /*
         * We disable caching for this query because returning the cached page 
         * will result in an infinite loop for the check below ... ask me how I know 😅😅
         */
        var pages = await contentRetriever.RetrievePagesByGuids<BlogPostPage>(
            [args.Guid],
            new RetrievePagesParameters { ChannelName = args.WebsiteChannelName, IsForPreview = true, LanguageName = args.ContentLanguageName, LinkedItemsMaxLevel = BlogPostPage.FullQueryDepth },
            RetrievePagesQueryParameters.Default,
            RetrievalCacheSettings.CacheDisabled);
        if (pages.FirstOrDefault() is not BlogPostPage page)
        {
            return;
        }

        // Do not re-process pages that already have a linked Q&A Discussion Page
        if (page.BlogPostPageQAndAQuestionPages.Any())
        {
            return;
        }

        var rootQuestionPage = (await contentRetriever
            .RetrievePages<QAndAQuestionsRootPage>(
                new RetrievePagesParameters { ChannelName = args.WebsiteChannelName, IsForPreview = false, LanguageName = args.ContentLanguageName }))
            .First();
        var now = clock.GetLocalNow();
        var questionMonthFolder = await mediator.Send(new QAndAMonthFolderQuery(rootQuestionPage, now.Year, now.Month));

        var url = await pageUrlRetriever.Retrieve(page);
        string postTitle = page.BasicItemTitle;
        string questionTitle = $"Blog Discussion: {postTitle}";
        string questionContent = $"""
            Blog Post: [{postTitle}]({url.RelativePathTrimmed()})

            Continue discussions 🤗 on this blog post below.
            """;
        var dxTopicTagIdentifiers = page.CoreTaxonomyDXTopics.Select(t => t.Identifier)
            ?? page.CoreTaxonomyDXTopics.Select(t => t.Identifier);
        var member = new CommunityMember()
        {
            Id = 0 // Only the Id is required and an Id of 0 will result in the author being the Kentico Community author
        };
        var command = new QAndAQuestionCreateCommand(
            member,
            questionMonthFolder,
            args.WebsiteChannelID,
            questionTitle,
            questionContent,
            SystemTaxonomies.QAndADiscussionTypeTaxonomy.BlogTag.GUID,
            dxTopicTagIdentifiers,
            page);

        await mediator.Send(command, default)
            .Map(async questionWebPageID =>
            {
                /*
                * This will recurse on this handler because it publishes the blog post
                * but we guard against updating a blog post page that already has a question path
                */
                return await mediator.Send(new BlogPostPageSetQuestionCommand(page, args.WebsiteChannelID, questionWebPageID));
            })
            .Match(
                MonadUtilities.Noop,
                err => log.LogError(nameof(BlogPostPublishCreateQAndAQuestionHandler), "UPDATE_BLOG_QUESTION", err));
    }
}
