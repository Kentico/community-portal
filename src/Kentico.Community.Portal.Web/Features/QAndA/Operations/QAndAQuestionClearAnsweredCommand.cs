using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionClearAnsweredCommand(QAndAQuestionPage QuestionPage) : ICommand<Result>;
public class QAndAQuestionClearAnsweredCommandHandler(
    WebPageCommandTools tools,
    IInfoProvider<UserInfo> users) : WebPageCommandHandler<QAndAQuestionClearAnsweredCommand, Result>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;

    public override async Task<Result> Handle(QAndAQuestionClearAnsweredCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetPublicMemberContentAuthor();
        var question = request.QuestionPage;

        var webPageManager = WebPageManagerFactory.Create(request.QuestionPage.SystemFields.WebPageItemWebsiteChannelId, user.UserID);

        bool create = await webPageManager.TryCreateDraft(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!create)
        {
            return Result.Failure($"Could not create a new draft for the question [{question.SystemFields.WebPageItemTreePath}]");
        }

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID), Guid.Empty },
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
