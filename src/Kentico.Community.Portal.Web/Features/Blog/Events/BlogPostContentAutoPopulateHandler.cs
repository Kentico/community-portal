using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using Kentico.Community.Portal.Core;
using Kentico.Xperience.Admin.Base.Authentication;

namespace Kentico.Community.Portal.Web.Features.Blog.Events;

public class BlogPostContentAutoPopulateHandler(
    IContentItemManagerFactory factory,
    IAuthenticatedUserAccessor userAccessor,
    IInfoProvider<ContentItemInfo> contentProvider,
    IContentItemCodeNameProvider nameProvider)
{
    private readonly IContentItemManagerFactory factory = factory;
    private readonly IAuthenticatedUserAccessor userAccessor = userAccessor;
    private readonly IInfoProvider<ContentItemInfo> contentProvider = contentProvider;
    private readonly IContentItemCodeNameProvider nameProvider = nameProvider;


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
        });

        var user = await userAccessor.Get();
        var manager = factory.Create(user.UserID);

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

        var user = await userAccessor.Get();
        var manager = factory.Create(user.UserID);
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
        });

        _ = await manager.TryCreateDraft(item.ContentItemID, args.ContentLanguageName);
        _ = await manager.TryUpdateDraft(item.ContentItemID, args.ContentLanguageName, data);
    }
}
