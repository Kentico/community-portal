using System.Text.RegularExpressions;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionUpdateCommand(
    QAndAQuestionPage Question,
    string UpdatedQuestionTitle,
    string UpdatedQuestionContent) : ICommand<Unit>;
public class QAndAQuestionUpdateCommandHandler : WebPageCommandHandler<QAndAQuestionUpdateCommand, Unit>
{
    private readonly IInfoProvider<UserInfo> users;
    private readonly ISystemClock clock;

    public QAndAQuestionUpdateCommandHandler(
        WebPageCommandTools tools,
        IInfoProvider<UserInfo> users,
        ISystemClock clock) : base(tools)
    {
        this.users = users;
        this.clock = clock;
    }

    public override async Task<Unit> Handle(QAndAQuestionUpdateCommand request, CancellationToken cancellationToken)
    {
        var question = request.Question;
        string filteredTitle = Regex.Replace(request.UpdatedQuestionTitle, @"[^a-zA-Z0-9\d]", "-").RemoveRepeatedCharacters('-');
        string displayName = filteredTitle[..Math.Min(100, filteredTitle.Length)];

        var user = await users.GetPublicMemberContentAuthor();

        var contentItemManager = ContentItemManagerFactory.Create(user.UserID);
        var webPageManager = WebPageManagerFactory.Create(WebsiteChannelContext.WebsiteChannelID, user.UserID);

        bool create = await webPageManager.TryCreateDraft(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!create)
        {
            throw new Exception($"Could not create a new draft for the question [{question.SystemFields.WebPageItemTreePath}]");
        }

        var metadata = await contentItemManager.GetContentItemLanguageMetadata(question.SystemFields.ContentItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        metadata.DisplayName = displayName;
        await contentItemManager.UpdateLanguageMetadata(metadata, cancellationToken);

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(QAndAQuestionPage.QAndAQuestionPageDateModified), clock.UtcNow },
            { nameof(QAndAQuestionPage.QAndAQuestionPageTitle), request.UpdatedQuestionTitle },
            // Content is not sanitized because it can included fenced code blocks.
            { nameof(QAndAQuestionPage.QAndAQuestionPageContent), request.UpdatedQuestionContent },
        });
        var draftData = new UpdateDraftData(itemData);
        bool update = await webPageManager.TryUpdateDraft(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, draftData, cancellationToken);
        if (!update)
        {
            throw new Exception($"Could not update the draft for the question [{question.SystemFields.WebPageItemTreePath}]");
        }

        bool publish = await webPageManager.TryPublish(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!publish)
        {
            throw new Exception($"Could not publish the draft for the question [{question.SystemFields.WebPageItemTreePath}]");
        }

        return Unit.Value;
    }
}
