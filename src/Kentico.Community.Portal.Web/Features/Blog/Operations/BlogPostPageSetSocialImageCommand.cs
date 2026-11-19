using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Features.SEO.SocialImages;

namespace Kentico.Community.Portal.Web.Features.Blog;

/// <summary>
/// Stores a generated social image as a published <see cref="ImageContent"/> item and
/// assigns it to the blog post's featured image, then republishes the post
/// </summary>
public record BlogPostPageSetSocialImageCommand(
    BlogPostPage BlogPost,
    int WebsiteChannelID,
    byte[] ImagePNG) : ICommand<Result>;
public class BlogPostPageSetSocialImageCommandHandler(
    WebPageCommandTools tools,
    IInfoProvider<UserInfo> users) : WebPageCommandHandler<BlogPostPageSetSocialImageCommand, Result>(tools)
{
    /// <summary>
    /// Content item display names are limited to 100 characters
    /// </summary>
    private const int DISPLAY_NAME_MAX_LENGTH = 100;

    private readonly IInfoProvider<UserInfo> users = users;

    public override async Task<Result> Handle(BlogPostPageSetSocialImageCommand request, CancellationToken cancellationToken)
    {
        var blogPost = request.BlogPost;
        var user = await users.GetPublicMemberContentAuthor();
        string title = blogPost.BasicItemTitle;

        var assetMetadata = new ContentItemAssetMetadata
        {
            Extension = SocialImage.EXTENSION,
            Identifier = Guid.NewGuid(),
            LastModified = DateTime.UtcNow,
            Name = $"{blogPost.SystemFields.WebPageItemName}-social{SocialImage.EXTENSION}",
            Size = request.ImagePNG.LongLength
        };
        var assetSource = new ContentItemAssetStreamSource(_ => Task.FromResult<Stream>(new MemoryStream(request.ImagePNG)));
        var imageData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(ImageContent.ImageContentAsset), new ContentItemAssetMetadataWithSource(assetSource, assetMetadata) },
            { nameof(ImageContent.BasicItemTitle), title },
            { nameof(ImageContent.BasicItemShortDescription), $"Social sharing image for the blog post \"{title}\"" },
        });

        string displayName = $"Social image - {title}";
        var createParameters = new CreateContentItemParameters(
            ImageContent.CONTENT_TYPE_NAME,
            null,
            displayName[..Math.Min(DISPLAY_NAME_MAX_LENGTH, displayName.Length)],
            PortalWebSiteChannel.DEFAULT_LANGUAGE,
            PortalWebSiteChannel.WORKSPACE_BLOG)
        {
            VersionStatus = VersionStatus.Published
        };

        var contentItemManager = ContentItemManagerFactory.Create(user.UserID);
        int imageID = await contentItemManager.Create(createParameters, imageData, cancellationToken);
        var imageQuery = new ContentItemQueryBuilder()
            .ForContentType(ImageContent.CONTENT_TYPE_NAME, q => q
                .Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemID), imageID))
                .Columns(nameof(ContentItemFields.ContentItemGUID)));
        var imageGUIDs = await Executor.GetResult(
            imageQuery,
            r => r.ContentItemGUID,
            new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = true },
            cancellationToken);
        var imageGUID = imageGUIDs.FirstOrDefault();
        if (imageGUID == default)
        {
            return Result.Failure($"Could not retrieve the created social image content item [{imageID}]");
        }

        var webPageManager = WebPageManagerFactory.Create(request.WebsiteChannelID, user.UserID);
        int webPageID = blogPost.SystemFields.WebPageItemID;

        if (!await webPageManager.TryCreateDraft(webPageID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken))
        {
            return Result.Failure($"Could not create a new draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]. Social image content item [{imageID}] is unassigned.");
        }

        var pageData = new ContentItemData([]);
        pageData.SetValue<IEnumerable<ContentItemReference>>(
            nameof(BlogPostPage.FeaturedImageImageContent),
            [new ContentItemReference { Identifier = imageGUID }]);

        if (!await webPageManager.TryUpdateDraft(webPageID, PortalWebSiteChannel.DEFAULT_LANGUAGE, new UpdateDraftData(pageData), cancellationToken))
        {
            return Result.Failure($"Could not update the draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]. Social image content item [{imageID}] is unassigned.");
        }

        if (!await webPageManager.TryPublish(webPageID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken))
        {
            return Result.Failure($"Could not publish the draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]");
        }

        return Result.Success();
    }
}
