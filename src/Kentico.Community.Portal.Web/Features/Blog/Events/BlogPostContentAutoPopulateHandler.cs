using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Blog.Events;

/// <summary>
/// Processes global events when BlogPostPage pages are created or updated and ensures
/// BlogPostContent reusable items are created or updated to keep the titles and taxonomies
/// in sync between the two items.
/// </summary>
/// <typeparam name="ContentItemInfo"></typeparam>
public class BlogPostContentAutoPopulateHandler(
    IInfoProvider<ContentItemInfo> contentProvider,
    IContentItemCodeNameProvider nameProvider,
    ContentItemManagerCreator contentItemManagerCreator,
    IContentQueryExecutor queryExecutor)
{
    private readonly IInfoProvider<ContentItemInfo> contentProvider = contentProvider;
    private readonly IContentItemCodeNameProvider nameProvider = nameProvider;
    private readonly ContentItemManagerCreator contentItemManagerCreator = contentItemManagerCreator;
    private readonly IContentQueryExecutor queryExecutor = queryExecutor;

    public async Task Handle(CreateWebPageEventArgs args)
    {
        if (!args.ContentItemData.TryGetValue<IEnumerable<ContentItemReference>>(nameof(BlogPostPage.BlogPostPageBlogPostContent), out var refs)
            || (refs is not null && refs.Any()))
        {
            return;
        }
        if (!args.ContentItemData.TryGetValue(nameof(BlogPostPage.BasicItemTitle), out string title)
            || string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        string name = await nameProvider.Get(title);
        var parameters = new CreateContentItemParameters(BlogPostContent.CONTENT_TYPE_NAME, name, title, args.ContentLanguageName, PortalWebSiteChannel.WORKSPACE_BLOG)
        {
            IsSecured = false,
            VersionStatus = VersionStatus.InitialDraft
        };
        var data = new ContentItemData(new Dictionary<string, object>
        {
            {
                nameof(BlogPostContent.BlogPostContentBlogType),
                args.ContentItemData.TryGetValue<IEnumerable<TagReference>>(nameof(BlogPostPage.BlogPostPageBlogType), out var blogType)
                    ? blogType
                    : []
            },
            {
                nameof(BlogPostContent.CoreTaxonomyDXTopics),
                args.ContentItemData.TryGetValue<IEnumerable<TagReference>>(nameof(BlogPostPage.CoreTaxonomyDXTopics), out var dxTopics)
                    ? dxTopics
                    : []
            }
        });

        var manager = await contentItemManagerCreator.GetContentItemManager();
        int contentItemID = await manager.Create(parameters, data);
        var item = await contentProvider.GetAsync(contentItemID);

        args.ContentItemData.SetValue<IEnumerable<ContentItemReference>>(
            nameof(BlogPostPage.BlogPostPageBlogPostContent),
            [new ContentItemReference { Identifier = item.ContentItemGUID }]);
    }

    public async Task Handle(UpdateWebPageDraftEventArgs args)
    {
        if (!args.ContentItemData.TryGetValue<IEnumerable<ContentItemReference>>(nameof(BlogPostPage.BlogPostPageBlogPostContent), out var refs)
            || refs.FirstOrDefault() is not ContentItemReference reference)
        {
            return;
        }

        // Query for the BlogPostContent item using the GUID
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                BlogPostContent.CONTENT_TYPE_NAME,
                queryParameters => queryParameters
                    .Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemGUID), reference.Identifier)));

        var blogPostContents = await queryExecutor.GetMappedResult<BlogPostContent>(
            query,
            new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = true });
        var blogPostContent = blogPostContents.FirstOrDefault();
        if (blogPostContent is null)
        {
            return;
        }

        var currentBlogType = args.ContentItemData.TryGetValue<IEnumerable<TagReference>>(nameof(BlogPostPage.BlogPostPageBlogType), out var blogType)
            ? blogType?.ToList() ?? []
            : [];
        var currentDXTopics = args.ContentItemData.TryGetValue<IEnumerable<TagReference>>(nameof(BlogPostPage.CoreTaxonomyDXTopics), out var dxTopics)
            ? dxTopics?.ToList() ?? []
            : [];

        // Get existing values from the BlogPostContent item
        var existingBlogType = blogPostContent.BlogPostContentBlogType?.ToList() ?? [];
        var existingDXTopics = blogPostContent.CoreTaxonomyDXTopics?.ToList() ?? [];

        // If all collections are identical, return early
        if (AreTagReferencesEqual(existingBlogType, currentBlogType) &&
            AreTagReferencesEqual(existingDXTopics, currentDXTopics))
        {
            return;
        }

        var manager = await contentItemManagerCreator.GetContentItemManager();
        _ = await manager.TryCreateDraft(blogPostContent.SystemFields.ContentItemID, args.ContentLanguageName);

        var data = new ContentItemData(new Dictionary<string, object>
        {
            {
                nameof(BlogPostContent.BlogPostContentBlogType),
                currentBlogType
            },
            {
                nameof(BlogPostContent.CoreTaxonomyDXTopics),
                currentDXTopics
            }
        });
        _ = await manager.TryUpdateDraft(blogPostContent.SystemFields.ContentItemID, args.ContentLanguageName, data);
    }

    /// <summary>
    /// Compares two collections of TagReference for equality by comparing their identifiers.
    /// </summary>
    /// <param name="first">First collection of TagReference objects</param>
    /// <param name="second">Second collection of TagReference objects</param>
    /// <returns>True if collections contain the same TagReference objects (by identifier), false otherwise</returns>
    private static bool AreTagReferencesEqual(IEnumerable<TagReference> first, IEnumerable<TagReference> second)
    {
        if (first is null && second is null)
        {
            return true;
        }

        if (first is null || second is null)
        {
            return false;
        }

        var firstIds = first.Select(x => x.Identifier).OrderBy(x => x).ToList();
        var secondIds = second.Select(x => x.Identifier).OrderBy(x => x).ToList();

        return firstIds.SequenceEqual(secondIds);
    }
}
