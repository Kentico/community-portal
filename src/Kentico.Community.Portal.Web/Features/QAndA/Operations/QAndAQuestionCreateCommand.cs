using System.Text.Json;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionCreateCommand(
    CommunityMember MemberAuthor,
    QAndAQuestionsRootPage QuestionParent,
    int WebsiteChannelID,
    string QuestionTitle,
    string QuestionContent,
    Guid DiscussionTypeTagIdentifier) : ICommand<int>;
public class QAndAQuestionCreateCommandHandler(
    WebPageCommandTools tools,
    IInfoProvider<UserInfo> users,
    ISystemClock clock) : WebPageCommandHandler<QAndAQuestionCreateCommand, int>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;
    private readonly ISystemClock clock = clock;

    public override async Task<int> Handle(QAndAQuestionCreateCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetPublicMemberContentAuthor();

        var webPageManager = WebPageManagerFactory.Create(request.WebsiteChannelID, user.UserID);

        string filteredTitle = QandAContentParser.Alphanumeric(request.QuestionTitle);
        string uniqueID = Guid.NewGuid().ToString("N");
        string displayName = $"{filteredTitle[..Math.Min(91, filteredTitle.Length)]}-{uniqueID[..8]}";
        string codeName = $"{filteredTitle[..Math.Min(41, filteredTitle.Length)]}-{uniqueID[..8]}";
        var now = clock.UtcNow;

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(QAndAQuestionPage.QAndAQuestionPageDateCreated), now },
            { nameof(QAndAQuestionPage.QAndAQuestionPageDateModified), now },
            // Content is not sanitized because it can include fenced code blocks.
            { nameof(QAndAQuestionPage.QAndAQuestionPageTitle), request.QuestionTitle },
            { nameof(QAndAQuestionPage.QAndAQuestionPageContent), request.QuestionContent },
            { nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID), request.MemberAuthor.Id },
            { nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID), Guid.Empty },
            { nameof(QAndAQuestionPage.QAndAQuestionPageDiscussionType), JsonSerializer.Serialize<IEnumerable<TagReference>>([new TagReference { Identifier = request.DiscussionTypeTagIdentifier }]) },
        });
        var contentItemParameters = new ContentItemParameters(QAndAQuestionPage.CONTENT_TYPE_NAME, itemData);
        var webPageParameters = new CreateWebPageParameters(codeName, displayName, PortalWebSiteChannel.DEFAULT_LANGUAGE, contentItemParameters)
        {
            ParentWebPageItemID = request.QuestionParent.SystemFields.WebPageItemID,
            RequiresAuthentication = false,
            VersionStatus = VersionStatus.Published
        };
        return await webPageManager.Create(webPageParameters, cancellationToken);
    }
}
