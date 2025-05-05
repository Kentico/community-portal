using System.Security.Claims;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Membership;

namespace Kentico.Community.Portal.Web.Features.Blog.Events;

public class BlogPostContentAutoPopulateHandler(
    IContentItemManagerFactory factory,
    AdminUserManager adminUserManager,
    IInfoProvider<UserInfo> userProvider,
    IHttpContextAccessor contextAccessor,
    IInfoProvider<ContentItemInfo> contentProvider,
    IContentItemCodeNameProvider nameProvider)
{
    private readonly IContentItemManagerFactory factory = factory;
    private readonly AdminUserManager adminUserManager = adminUserManager;
    private readonly IInfoProvider<UserInfo> userProvider = userProvider;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
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

        var manager = await GetManager();
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

        if (!item.TryGetValue(nameof(BlogPostContent.BlogPostContentBlogType), out object existingBlogTypeObj)
            || existingBlogTypeObj is not IEnumerable<TagReference> existingBlogType
            || !item.TryGetValue(nameof(BlogPostContent.BlogPostContentDXTopics), out object existingDXTopicsObj)
            || existingDXTopicsObj is not IEnumerable<TagReference> existingDXTopics
            || (!existingBlogType.Except(currentBlogType).Any()
                && !existingDXTopics.Except(currentDXTopics).Any()))
        {
            return;
        }

        var manager = await GetManager();
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
        });
        _ = await manager.TryUpdateDraft(item.ContentItemID, args.ContentLanguageName, data);
    }

    public async Task<IContentItemManager> GetManager()
    {
        /*
         * Scheduled posts are published in a background thread
         * which means there's no HttpContext
         */
        if (contextAccessor.HttpContext?.User is not ClaimsPrincipal principal)
        {
            var contentAuthor = await userProvider.GetPublicMemberContentAuthor();
            return factory.Create(contentAuthor.UserID);
        }

        var adminUser = await adminUserManager.GetUserAsync(principal);

        if (adminUser is null)
        {
            var contentAuthor = await userProvider.GetPublicMemberContentAuthor();
            return factory.Create(contentAuthor.UserID);
        }

        return factory.Create(adminUser.UserID);
    }
}
