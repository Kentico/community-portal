using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Blog.Events;

public class BlogPostContentAutoPopulateHandler(
    IInfoProvider<ContentItemInfo> contentProvider,
    IContentItemCodeNameProvider nameProvider,
    ContentItemManagerCreator contentItemManagerCreator)
{
    private readonly IInfoProvider<ContentItemInfo> contentProvider = contentProvider;
    private readonly IContentItemCodeNameProvider nameProvider = nameProvider;
    private readonly ContentItemManagerCreator contentItemManagerCreator = contentItemManagerCreator;

    public async Task Handle(CreateWebPageEventArgs args)
    {
        if (!args.ContentItemData.TryGetValue<IEnumerable<ContentItemReference>>(nameof(BlogPostPage.BlogPostPageBlogPostContent), out var refs)
            || (refs is not null && refs.Any()))
        {
            return;
        }
        if (!args.ContentItemData.TryGetValue(nameof(BlogPostPage.WebPageMetaTitle), out string title))
        {
            return;
        }

        string name = await nameProvider.Get(title);
        var parameters = new CreateContentItemParameters(BlogPostContent.CONTENT_TYPE_NAME, name, title, args.ContentLanguageName, PortalWebSiteChannel.DEFAULT_WORKSPACE)
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
                nameof(BlogPostContent.BlogPostContentDXTopics),
                args.ContentItemData.TryGetValue<IEnumerable<TagReference>>(nameof(BlogPostPage.BlogPostPageDXTopics), out var dxTopics)
                    ? dxTopics
                    : []
            },
            {
                nameof(BlogPostContent.CoreTaxonomyDXTopics),
                dxTopics ?? []
            },
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

        var item = await contentProvider.GetAsync(reference.Identifier);
        if (item is null)
        {
            return;
        }

        var currentBlogType = args.ContentItemData.TryGetValue<IEnumerable<TagReference>>(nameof(BlogPostPage.BlogPostPageBlogType), out var blogType)
            ? blogType
            : [];
        var currentDXTopics = args.ContentItemData.TryGetValue<IEnumerable<TagReference>>(nameof(BlogPostPage.BlogPostPageDXTopics), out var dxTopics)
            ? dxTopics
            : [];

        // If all are identical, return early
        if (item.TryGetValue(nameof(BlogPostContent.BlogPostContentBlogType), out object existingBlogTypeObj)
            && existingBlogTypeObj is IEnumerable<TagReference> existingBlogType && !existingBlogType.Except(currentBlogType).Any()
            && item.TryGetValue(nameof(BlogPostContent.BlogPostContentDXTopics), out object existingDXTopicsObj)
            && existingDXTopicsObj is IEnumerable<TagReference> existingDXTopics && !existingDXTopics.Except(currentDXTopics).Any())
        {
            return;
        }

        var manager = await contentItemManagerCreator.GetContentItemManager();
        _ = await manager.TryCreateDraft(item.ContentItemID, args.ContentLanguageName);

        var data = new ContentItemData(new Dictionary<string, object>
        {
            {
                nameof(BlogPostContent.BlogPostContentBlogType),
                currentBlogType
            },
            {
                nameof(BlogPostContent.BlogPostContentDXTopics),
                currentDXTopics
            },
            {
                nameof(BlogPostContent.CoreTaxonomyDXTopics),
                currentDXTopics
            },
        });
        _ = await manager.TryUpdateDraft(item.ContentItemID, args.ContentLanguageName, data);
    }
}
