using System.Text.Json;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionCreateCommand(
    CommunityMember MemberAuthor,
    WebPageFolder QuestionParent,
    int WebsiteChannelID,
    string QuestionTitle,
    string QuestionContent,
    Guid DiscussionTypeTagIdentifier,
    IEnumerable<Guid> DXTopics,
    Maybe<BlogPostPage> LinkedBlogPost) : ICommand<Result<int>>;
public class QAndAQuestionCreateCommandHandler(
    WebPageCommandTools tools,
    IInfoProvider<UserInfo> users,
    ITaxonomyRetriever taxonomyRetriever,
    ISystemClock clock) : WebPageCommandHandler<QAndAQuestionCreateCommand, Result<int>>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private readonly ISystemClock clock = clock;

    public override async Task<Result<int>> Handle(QAndAQuestionCreateCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetPublicMemberContentAuthor();

        var webPageManager = WebPageManagerFactory.Create(request.WebsiteChannelID, user.UserID);

        string filteredTitle = QandAContentParser.Alphanumeric(request.QuestionTitle);
        string uniqueID = Guid.NewGuid().ToString("N");
        string displayName = $"{filteredTitle[..Math.Min(91, filteredTitle.Length)]}-{uniqueID[..8]}";
        var dxTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.DXTopicTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var validTags = dxTaxonomy
            .Tags
            .Where(t => request.DXTopics.Contains(t.Identifier))
            .Select(t => new TagReference() { Identifier = t.Identifier })
            .ToList();
        var now = clock.UtcNow;

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(QAndAQuestionPage.QAndAQuestionPageDateCreated), now },
            { nameof(QAndAQuestionPage.QAndAQuestionPageDateModified), now },
            { nameof(QAndAQuestionPage.QAndAQuestionPageTitle), request.QuestionTitle },
            // Content is not sanitized because it can include fenced code blocks.
            { nameof(QAndAQuestionPage.QAndAQuestionPageContent), request.QuestionContent },
            { nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID), request.MemberAuthor.Id },
            { nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID), Guid.Empty },
            {
                nameof(QAndAQuestionPage.QAndAQuestionPageDiscussionType),
                JsonSerializer.Serialize<IEnumerable<TagReference>>([new() { Identifier = request.DiscussionTypeTagIdentifier }])
            },
            { nameof(QAndAQuestionPage.QAndAQuestionPageDXTopics), JsonSerializer.Serialize(validTags) }
        });
        request.LinkedBlogPost
            .Execute(post =>
            {
                itemData.SetValue(
                    nameof(QAndAQuestionPage.QAndAQuestionPageBlogPostPage),
                    JsonSerializer.Serialize<IEnumerable<WebPageRelatedItem>>([new() { WebPageGuid = post.SystemFields.WebPageItemGUID }]));
            });

        var contentItemParameters = new ContentItemParameters(QAndAQuestionPage.CONTENT_TYPE_NAME, itemData);
        var webPageParameters = new CreateWebPageParameters(displayName, PortalWebSiteChannel.DEFAULT_LANGUAGE, contentItemParameters)
        {
            ParentWebPageItemID = request.QuestionParent.WebPageItemID,
            RequiresAuthentication = false,
            VersionStatus = VersionStatus.Published
        };

        try
        {
            return await webPageManager.Create(webPageParameters);

        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Could not create new question: {ex}");
        }
    }
}
