using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA.Notifications;

public class QAndANotificationSettingsManager(
    IInfoProvider<DiscussionMemberNotificationSettingsInfo> settingsProvider,
    IInfoProvider<DiscussionEventInfo> eventProvider,
    IInfoProvider<DiscussionMemberSubscriptionInfo> subscriptionProvider,
    TimeProvider clock,
    ILogger<QAndANotificationSettingsManager> logger)
{
    private readonly IInfoProvider<DiscussionMemberNotificationSettingsInfo> settingsProvider = settingsProvider;
    private readonly IInfoProvider<DiscussionEventInfo> eventProvider = eventProvider;
    private readonly IInfoProvider<DiscussionMemberSubscriptionInfo> subscriptionProvider = subscriptionProvider;
    private readonly TimeProvider clock = clock;
    private readonly ILogger<QAndANotificationSettingsManager> logger = logger;

    public async Task<Result> SubscribeToDiscussion(int memberID, QAndAQuestionPage page)
    {
        try
        {
            using var transaction = new CMSTransactionScope();
            var notifications = await GetSubscriptionsByMemberID(memberID);
            var memberSettings = await GetSettingsByMemberID(memberID)
                .GetValueOrDefault(() => InitializeNewSettings(memberID));

            bool isNewSettings = memberSettings.DiscussionMemberNotificationSettingsID == 0;

            if (notifications
                .Select(i => i.DiscussionMemberSubscriptionWebPageItemID)
                .Contains(page.SystemFields.WebPageItemID))
            {
                return Result.Success();
            }

            var newSubscription = new DiscussionMemberSubscriptionInfo
            {
                DiscussionMemberSubscriptionMemberID = memberID,
                DiscussionMemberSubscriptionWebPageItemID = page.SystemFields.WebPageItemID
            };

            // Only set last event ID if this is brand new settings (first time subscribing to anything)
            // Otherwise, keep existing lastEventID - let processor update it after sending notifications
            if (isNewSettings)
            {
                memberSettings.DiscussionMemberNotificationSettingsLatestDiscussionEventID = await GetLatestEvent()
                    .GetValueOrDefault(info => info.DiscussionEventID, 0);
            }

            await subscriptionProvider.SetAsync(newSubscription);

            // Only save settings if new, otherwise no need to update them
            if (isNewSettings)
            {
                await settingsProvider.SetAsync(memberSettings);
            }

            transaction.Commit();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to subscribe to question {QuestionID} for member {MemberID}",
                page.SystemFields.WebPageItemID, memberID);
            return Result.Failure($"Failed to subscribe to question: {ex.Message}");
        }
    }

    public async Task<Result> UnsubscribeFromDiscussion(int memberID, QAndAQuestionPage page)
    {
        try
        {
            using var transaction = new CMSTransactionScope();
            var subscriptions = await GetSubscriptionsByMemberID(memberID);

            var existingSubscription = subscriptions.FirstOrDefault(n => n.DiscussionMemberSubscriptionWebPageItemID == page.SystemFields.WebPageItemID);

            if (existingSubscription is null)
            {
                return Result.Success();
            }

            // Don't update last event ID when unsubscribing - just remove from list
            // Let processor update lastEventID after sending notifications
            await subscriptionProvider.DeleteAsync(existingSubscription);
            transaction.Commit();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unsubscribe from question {QuestionID} for member {MemberID}",
                page.SystemFields.WebPageItemID, memberID);
            return Result.Failure($"Failed to unsubscribe from question: {ex.Message}");
        }
    }

    public Task<QAndANotificationFrequencyType> GetMemberFrequency(int memberID) =>
        GetSettingsByMemberID(memberID)
            .Map(s => Enum.TryParse<QAndANotificationFrequencyType>(s.DiscussionMemberNotificationSettingsFrequencyType, out var frequencyType)
                ? frequencyType
                : QAndANotificationFrequencyType.Weekly)
            .GetValueOrDefault(() => QAndANotificationFrequencyType.Weekly);

    public Task<bool> GetAutoSubscribeEnabled(int memberID) =>
        GetSettingsByMemberID(memberID)
            .Map(s => s.DiscussionMemberNotificationSettingsAutoSubscribeEnabled)
            .GetValueOrDefault(() => false);

    private Task<Maybe<DiscussionMemberNotificationSettingsInfo>> GetSettingsByMemberID(int memberID) =>
        settingsProvider.Get()
            .WhereEquals(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsMemberID), memberID)
            .GetEnumerableTypedResultAsync()
            .TryFirst();

    private async Task<List<DiscussionMemberSubscriptionInfo>> GetSubscriptionsByMemberID(int memberID) =>
        [.. await subscriptionProvider.Get()
            .WhereEquals(nameof(DiscussionMemberSubscriptionInfo.DiscussionMemberSubscriptionMemberID), memberID)
            .GetEnumerableTypedResultAsync()];

    private Task<Maybe<DiscussionEventInfo>> GetLatestEvent() =>
        eventProvider
            .Get()
            .OrderBy(OrderDirection.Descending, nameof(DiscussionEventInfo.DiscussionEventID))
            .TopN(1)
            .GetEnumerableTypedResultAsync()
            .TryFirst();

    public async Task<Result> UpdateMemberSettings(int memberID, QAndANotificationFrequencyType frequency, bool autoSubscribeEnabled)
    {
        try
        {
            using var transaction = new CMSTransactionScope();

            var info = await GetSettingsByMemberID(memberID)
                .Match(i => i, () => InitializeNewSettings(memberID));

            info.DiscussionMemberNotificationSettingsFrequencyType = Enum.GetName(frequency);
            info.DiscussionMemberNotificationSettingsAutoSubscribeEnabled = autoSubscribeEnabled;

            await settingsProvider.SetAsync(info);

            transaction.Commit();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update settings for member {MemberID}",
                memberID);
            return Result.Failure($"Failed to update notification settings: {ex.Message}");
        }
    }

    public async Task<Result> SetAutoSubscribeEnabled(int memberID, bool enabled)
    {
        try
        {
            using var transaction = new CMSTransactionScope();

            var info = await GetSettingsByMemberID(memberID)
                .Match(i => i, () => InitializeNewSettings(memberID));

            info.DiscussionMemberNotificationSettingsAutoSubscribeEnabled = enabled;

            await settingsProvider.SetAsync(info);

            transaction.Commit();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set auto-subscribe {Enabled} for member {MemberID}",
                enabled, memberID);
            return Result.Failure($"Failed to set auto-subscribe setting: {ex.Message}");
        }
    }

    private DiscussionMemberNotificationSettingsInfo InitializeNewSettings(int memberID) =>
        new()
        {
            DiscussionMemberNotificationSettingsMemberID = memberID,
            DiscussionMemberNotificationSettingsLatestDiscussionEventID = 0,
            DiscussionMemberNotificationSettingsDateCreated = clock.GetLocalNow().DateTime,
            DiscussionMemberNotificationSettingsDateModified = clock.GetLocalNow().DateTime,
            DiscussionMemberNotificationSettingsFrequencyType = Enum.GetName(QAndANotificationFrequencyType.Weekly),
            DiscussionMemberNotificationSettingsAutoSubscribeEnabled = false
        };

    public async Task<IEnumerable<int>> GetSubscribedWebPageItemIDs(CommunityMember member)
    {
        var subscriptions = await GetSubscriptionsByMemberID(member.Id);

        return subscriptions.Select(n => n.DiscussionMemberSubscriptionWebPageItemID);
    }
}
