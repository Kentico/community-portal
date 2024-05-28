using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionMarkAnsweredCommand(QAndAQuestionPage QuestionPage, QAndAAnswerDataInfo Answer, int ChannelID) : ICommand<Unit>;
public class QAndAQuestionMarkAnsweredCommandHandler(
    WebPageCommandTools tools,
    IInfoProvider<UserInfo> users) : WebPageCommandHandler<QAndAQuestionMarkAnsweredCommand, Unit>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;

    public override async Task<Unit> Handle(QAndAQuestionMarkAnsweredCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetPublicMemberContentAuthor();
        var question = request.QuestionPage;

        var webPageManager = WebPageManagerFactory.Create(request.ChannelID, user.UserID);

        bool create = await webPageManager.TryCreateDraft(question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        if (!create)
        {
            throw new Exception($"Could not create a new draft for the question [{question.SystemFields.WebPageItemTreePath}]");
        }

        var itemData = new ContentItemData(new Dictionary<string, object>
        {
            { nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID), request.Answer.QAndAAnswerDataGUID },
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
