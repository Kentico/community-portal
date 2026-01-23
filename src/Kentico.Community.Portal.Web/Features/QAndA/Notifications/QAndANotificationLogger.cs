using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.QAndA.Notifications;

public class QAndANotificationLogger(
    IInfoProvider<DiscussionEventInfo> eventProvider,
    TimeProvider clock)
{
    public Task NotifyNewAnswer(QAndAAnswerDataInfo answer) =>
        Log(answer, QAndANotificationEventType.ResponseCreated);

    public Task NotifyAcceptedAnswer(QAndAAnswerDataInfo answer) =>
        Log(answer, QAndANotificationEventType.AnswerAccepted);

    private async Task Log(QAndAAnswerDataInfo answer, QAndANotificationEventType notificationEvent)
    {
        var log = new DiscussionEventInfo
        {
            DiscussionEventDateModified = clock.GetUtcNow().DateTime,
            DiscussionEventType = Enum.GetName(notificationEvent),
            DiscussionEventWebPageItemID = answer.QAndAAnswerDataQuestionWebPageItemID,
            DiscussionEventWebsiteChannelID = answer.QAndAAnswerDataWebsiteChannelID,
            DiscussionEventFromMemberID = answer.QAndAAnswerDataAuthorMemberID,
        };
        await eventProvider.SetAsync(log);
    }
}
