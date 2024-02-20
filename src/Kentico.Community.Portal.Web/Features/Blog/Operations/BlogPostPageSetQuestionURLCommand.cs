using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPageUpdateCommand(
    BlogPostPage BlogPost,
    int WebsiteChannelID,
    WebPageUrl QuestionPageURL) : ICommand<Unit>;
public class BlogPostPageUpdateCommandHandler(
    WebPageCommandTools tools,
    IInfoProvider<UserInfo> users) : WebPageCommandHandler<BlogPostPageUpdateCommand, Unit>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;

    public override async Task<Unit> Handle(BlogPostPageUpdateCommand request, CancellationToken cancellationToken)
    {
        var blogPost = request.BlogPost;
        var user = await users.GetPublicMemberContentAuthor();
        var webPageManager = WebPageManagerFactory.Create(request.WebsiteChannelID, user.UserID);

        bool create = await webPageManager.TryCreateDraft(blogPost.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!create)
        {
            throw new Exception($"Could not create a new draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]");
        }

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(BlogPostPage.BlogPostPageQAndADiscussionLinkPath), request.QuestionPageURL.RelativePathTrimmed() },
        });
        var draftData = new UpdateDraftData(itemData);
        bool update = await webPageManager.TryUpdateDraft(blogPost.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, draftData, cancellationToken);
        if (!update)
        {
            throw new Exception($"Could not update the draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]");
        }

        bool publish = await webPageManager.TryPublish(blogPost.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!publish)
        {
            throw new Exception($"Could not publish the draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]");
        }

        return Unit.Value;
    }
}
