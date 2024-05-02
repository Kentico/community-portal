using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPageSetQuestionURLCommand(
    BlogPostPage BlogPost,
    int WebsiteChannelID,
    WebPageUrl QuestionPageURL) : ICommand<Result>;
public class BlogPostPageSetQuestionURLCommandHandler(
    WebPageCommandTools tools,
    IInfoProvider<UserInfo> users) : WebPageCommandHandler<BlogPostPageSetQuestionURLCommand, Result>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;

    public override async Task<Result> Handle(BlogPostPageSetQuestionURLCommand request, CancellationToken cancellationToken)
    {
        var blogPost = request.BlogPost;
        var user = await users.GetPublicMemberContentAuthor();
        var webPageManager = WebPageManagerFactory.Create(request.WebsiteChannelID, user.UserID);

        bool create = await webPageManager.TryCreateDraft(blogPost.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!create)
        {
            return Result.Failure($"Could not create a new draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]");
        }

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(BlogPostPage.BlogPostPageQAndADiscussionLinkPath), request.QuestionPageURL.RelativePathTrimmed() },
        });
        var draftData = new UpdateDraftData(itemData);
        bool update = await webPageManager.TryUpdateDraft(blogPost.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, draftData, cancellationToken);
        if (!update)
        {
            return Result.Failure($"Could not update the draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]");
        }

        bool publish = await webPageManager.TryPublish(blogPost.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!publish)
        {
            return Result.Failure($"Could not publish the draft for the blog post [{blogPost.SystemFields.WebPageItemTreePath}]");
        }

        return Result.Success();
    }
}
