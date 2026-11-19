using CMS.Base;
using Kentico.Community.Portal.Web.Features.SEO.SocialImages;
using Kentico.Content.Web.Mvc;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.Blog.Events;

/// <summary>
/// Generates a branded social image for a published <see cref="BlogPostPage"/> that has no featured image.
/// Editor-selected featured images are never replaced.
/// </summary>
/// <remarks>
/// Thin glue over <see cref="ISocialImageGenerator"/>. Every failure is logged and swallowed:
/// an exception here must never fail or roll back the publish that raised the event.
/// </remarks>
public class BlogPostPublishSocialImageHandler(
    IServiceScopeFactory scopeFactory,
    ISocialImageGenerator generator,
    ILogger<BlogPostPublishSocialImageHandler> logger) : IAsyncEventHandler<AfterPublishWebPageEvent>
{
    private static readonly EventId eventId = new(0, "BLOG_SOCIAL_IMAGE");

    private readonly IServiceScopeFactory scopeFactory = scopeFactory;
    private readonly ISocialImageGenerator generator = generator;
    private readonly ILogger<BlogPostPublishSocialImageHandler> logger = logger;

    public async Task HandleAsync(AfterPublishWebPageEvent asyncEvent, CancellationToken cancellationToken)
    {
        var data = asyncEvent.Data;
        if (!string.Equals(data.ContentTypeName, BlogPostPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            await Handle(data, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(eventId, ex, "Social image generation failed for blog post {WebPageItemGUID}. The post stays published and falls back to the site logo.", data.Guid);
        }
    }

    private async Task Handle(PublishWebPageEventData data, CancellationToken cancellationToken)
    {
        // Handlers are singletons, content APIs are scoped
        await using var scope = scopeFactory.CreateAsyncScope();
        var contentRetriever = scope.ServiceProvider.GetRequiredService<IContentRetriever>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        /*
         * Caching is disabled: this handler republishes the post, which raises this event again,
         * and only the latest data shows the featured image it just assigned
         */
        var pages = await contentRetriever.RetrievePagesByGuids<BlogPostPage>(
            [data.Guid],
            new RetrievePagesParameters { ChannelName = data.WebsiteChannelName, IsForPreview = true, LanguageName = data.ContentLanguageName, LinkedItemsMaxLevel = 1 },
            RetrievePagesQueryParameters.Default,
            RetrievalCacheSettings.CacheDisabled,
            cancellationToken);
        if (pages.FirstOrDefault() is not BlogPostPage page || page.FeaturedImageImageContent.Any())
        {
            return;
        }

        var byline = Maybe.From(page.BlogPostPageAuthorContent.FirstOrDefault())
            .Map(a => $"{a.AuthorContentFirstName} {a.AuthorContentSurname}".Trim())
            .MapNullOrWhiteSpaceAsNone();

        var image = generator.Generate(new SocialImageRequest(page.BasicItemTitle, byline));
        if (image.IsFailure)
        {
            logger.LogWarning(eventId, "Could not generate a social image for blog post {WebPageItemGUID}: {Error}", data.Guid, image.Error);
            return;
        }

        var result = await mediator.Send(new BlogPostPageSetSocialImageCommand(page, data.WebsiteChannelID, image.Value), cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning(eventId, "Could not store the social image for blog post {WebPageItemGUID}: {Error}", data.Guid, result.Error);
            return;
        }

        logger.LogInformation(eventId, "Generated social image for blog post {WebPageItemGUID}", data.Guid);
    }
}
