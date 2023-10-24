using System.Text.RegularExpressions;
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
    string QuestionTitle,
    string QuestionContent) : ICommand<int>;
public class QAndAQuestionCreateCommandHandler : WebPageCommandHandler<QAndAQuestionCreateCommand, int>
{
    private readonly IInfoProvider<UserInfo> users;
    private readonly ISystemClock clock;

    public QAndAQuestionCreateCommandHandler(
        WebPageCommandTools tools,
        IInfoProvider<UserInfo> users,
        ISystemClock clock) : base(tools)
    {
        this.users = users;
        this.clock = clock;
    }

    public override async Task<int> Handle(QAndAQuestionCreateCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetPublicMemberContentAuthor();

        var webPageManager = WebPageManagerFactory.Create(WebsiteChannelContext.WebsiteChannelID, user.UserID);

        string filteredTitle = Regex.Replace(request.QuestionTitle, @"[^a-zA-Z0-9\d]", "-").RemoveRepeatedCharacters('-');
        string displayName = filteredTitle[..Math.Min(100, filteredTitle.Length)];
        string uniqueID = Guid.NewGuid().ToString("N");
        string codeName = $"{filteredTitle[..Math.Min(42, filteredTitle.Length)]}{uniqueID[..8]}";
        var now = clock.UtcNow;

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(QAndAQuestionPage.QAndAQuestionPageDateCreated), now },
            { nameof(QAndAQuestionPage.QAndAQuestionPageDateModified), now },
            // Content is not sanitized because it can included fenced code blocks.
            { nameof(QAndAQuestionPage.QAndAQuestionPageTitle), request.QuestionTitle },
            { nameof(QAndAQuestionPage.QAndAQuestionPageContent), request.QuestionContent },
            { nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID), request.MemberAuthor.Id },
            { nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID), Guid.Empty },
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
