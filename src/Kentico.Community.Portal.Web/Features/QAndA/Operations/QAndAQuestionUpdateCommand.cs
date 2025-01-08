using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionUpdateCommand(
    QAndAQuestionPage Question,
    string UpdatedQuestionTitle,
    string UpdatedQuestionContent,
    IReadOnlyList<string> DXTopics,
    int ChannelID) : ICommand<Result>;
public class QAndAQuestionUpdateCommandHandler(
    WebPageCommandTools tools,
    IInfoProvider<UserInfo> users,
    ITaxonomyRetriever taxonomyRetriever,
    ISystemClock clock) : WebPageCommandHandler<QAndAQuestionUpdateCommand, Result>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private readonly ISystemClock clock = clock;

    public override async Task<Result> Handle(QAndAQuestionUpdateCommand request, CancellationToken cancellationToken)
    {
        var question = request.Question;
        string filteredTitle = QandAContentParser.Alphanumeric(request.UpdatedQuestionTitle);
        string uniqueID = Guid.NewGuid().ToString("N");
        string displayName = $"{filteredTitle[..Math.Min(91, filteredTitle.Length)]}-{uniqueID[..8]}";
        var dxTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.DXTopicTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var validTags = dxTaxonomy
            .Tags
            .Where(t => request.DXTopics.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
            .Select(t => new TagReference() { Identifier = t.Identifier })
            .ToList();

        var user = await users.GetPublicMemberContentAuthor();
        var webPageManager = WebPageManagerFactory.Create(request.ChannelID, user.UserID);

        bool create = await webPageManager.TryCreateDraft(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!create)
        {
            // Creation of a draft could fail because a draft already exists, so let's discard and try again
            bool discard = await webPageManager.TryDiscardDraft(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
            if (!discard)
            {
                return Result.Failure($"Could not discard the draft for the question [{question.SystemFields.WebPageItemTreePath}]");
            }

            // If we still couldn't create a draft then something is wrong we can't recover from
            create = await webPageManager.TryCreateDraft(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
            if (!create)
            {
                return Result.Failure($"Could not create a new draft for the question [{question.SystemFields.WebPageItemTreePath}]");
            }
        }

        var metadata = await webPageManager.GetContentItemLanguageMetadata(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        metadata.DisplayName = displayName;
        await webPageManager.UpdateLanguageMetadata(metadata, cancellationToken);

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(QAndAQuestionPage.QAndAQuestionPageDateModified), clock.UtcNow },
            { nameof(QAndAQuestionPage.QAndAQuestionPageTitle), request.UpdatedQuestionTitle },
            // Content is not sanitized because it can include fenced code blocks.
            { nameof(QAndAQuestionPage.QAndAQuestionPageContent), request.UpdatedQuestionContent },
            { nameof(QAndAQuestionPage.QAndAQuestionPageDXTopics), validTags }
        });
        var draftData = new UpdateDraftData(itemData);
        bool update = await webPageManager.TryUpdateDraft(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, draftData, cancellationToken);
        if (!update)
        {
            return Result.Failure($"Could not update the draft for the question [{question.SystemFields.WebPageItemTreePath}]");
        }

        bool publish = await webPageManager.TryPublish(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!publish)
        {
            return Result.Failure($"Could not publish the draft for the question [{question.SystemFields.WebPageItemTreePath}]");
        }

        return Result.Success();
    }
}
