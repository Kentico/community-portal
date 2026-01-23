using System.ComponentModel;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.EmailLibrary;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA.Notifications;

public class QAndANotificationsProcessor(
    IContentRetriever contentRetriever,
    TimeProvider clock,
    IMemberEmailService emailService,
    IInfoProvider<DiscussionMemberNotificationSettingsInfo> settingsProvider,
    IInfoProvider<DiscussionEventInfo> eventProvider,
    IInfoProvider<DiscussionMemberSubscriptionInfo> notificationsProvider,
    IInfoProvider<MemberInfo> memberInfoProvider,
    IRazorComponentRenderer componentRenderer,
    IWebPageUrlRetriever webPageUrlRetriever,
    IEmailChannelDomainProvider emailChannelDomainProvider,
    IInfoProvider<EmailChannelInfo> emailChannelProvider,
    ILogger<QAndANotificationsProcessor> logger)
{
    private const int EVENT_RETENTION_MONTHS = 3;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly TimeProvider clock = clock;
    private readonly IMemberEmailService emailService = emailService;
    private readonly IInfoProvider<DiscussionMemberNotificationSettingsInfo> settingsProvider = settingsProvider;
    private readonly IInfoProvider<DiscussionEventInfo> eventProvider = eventProvider;
    private readonly IInfoProvider<DiscussionMemberSubscriptionInfo> notificationsProvider = notificationsProvider;
    private readonly IInfoProvider<MemberInfo> memberInfoProvider = memberInfoProvider;
    private readonly IRazorComponentRenderer componentRenderer = componentRenderer;
    private readonly IWebPageUrlRetriever webPageUrlRetriever = webPageUrlRetriever;
    private readonly IEmailChannelDomainProvider emailChannelDomainProvider = emailChannelDomainProvider;
    private readonly IInfoProvider<EmailChannelInfo> emailChannelProvider = emailChannelProvider;
    private readonly ILogger<QAndANotificationsProcessor> logger = logger;

    public async Task<Result> Process(QAndANotificationFrequencyType frequency)
    {
        var batches = await CollectNotificationContent(frequency);
        string baseUrl = await GetBaseUrl();

        int totalNotificationsSent = 0;
        var successfulBatches = new List<(CommunityMember Member, int LastEventID)>();

        foreach (var batch in batches)
        {
            if (batch.Notifications.Count == 0)
            {
                continue;
            }

            string notificationsHTML = await componentRenderer.RenderToStringAsync<QAndANotificationEmailComponent>(
                new Dictionary<string, object?>
                {
                    { nameof(QAndANotificationEmailComponent.Notifications), batch.Notifications },
                    { nameof(QAndANotificationEmailComponent.Member), batch.Member }
                });

            var emailData = new MemberEmailConfiguration.DiscussionNotificationEmailData(notificationsHTML, batch.Notifications.Count, baseUrl, frequency);

            try
            {
                await emailService.SendEmail(batch.Member, MemberEmailConfiguration.QAndADiscussionNotification(emailData));
                successfulBatches.Add((batch.Member, batch.LastProcessedEventID));
                totalNotificationsSent += batch.Notifications.Count;
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(0, "NOTIFICATION_EMAIL_SEND_FAILURE"), ex, "Failed to send notification email to {MemberID} for {NotificationFrequency}", batch.Member.Id, frequency);
            }
        }

        // Update member settings and cleanup events (only for successful emails)
        if (successfulBatches.Count > 0)
        {
            foreach (var (member, lastEventID) in successfulBatches)
            {
                try
                {
                    _ = await UpdateMemberNotificationSettings(member, lastEventID, frequency);
                }
                catch (Exception ex)
                {
                    logger.LogError(new EventId(0, "NOTIFICATION_SETTINGS_UPDATE_FAILURE"), ex, "Failed to update notification settings for {MemberID} and {NotificationFrequency}", member.Id, frequency);
                }
            }

            // Cleanup old events after all members successfully processed
            eventProvider.BulkDelete(
                new WhereCondition(nameof(DiscussionEventInfo.DiscussionEventDateModified), QueryOperator.LessThan, clock.GetUtcNow().AddMonths(-EVENT_RETENTION_MONTHS)),
                new BulkDeleteSettings { RemoveDependencies = false });
        }

        logger.LogInformation(new EventId(0, "NOTIFICATION_EMAILS_SENT"), "Processed {NotificationCount} notifications across {MemberCount} members for {NotificationFrequency}", totalNotificationsSent, successfulBatches.Count, frequency);

        return Result.Success();
    }

    public async Task<IReadOnlyList<QAndAMemberNotificationsBatch>> CollectNotificationContent(QAndANotificationFrequencyType frequency)
    {
        var memberSettings = await GetAllSettingsAtFrequency(frequency);
        if (memberSettings.Count == 0)
        {
            return [];
        }

        var (limitDate, notifiableEvents) = await GetNotifiableEvents(frequency);
        int[] questionWebPageIDs = [.. notifiableEvents.Select(x => x.DiscussionEventWebPageItemID).Distinct()];
        var questionsLookup = await GetQuestionsByWebPageItemID(questionWebPageIDs);
        var subscribedQuestionsByMemberID = await GetSubscribedQuestionsGroupedByMember(memberSettings, questionWebPageIDs);

        var eventsByMember = FilterEventsForMembers(memberSettings, notifiableEvents, limitDate, subscribedQuestionsByMemberID, questionsLookup);
        var membersLookup = await LoadMembersLookup(eventsByMember, questionsLookup);

        return await BuildNotificationBatches(eventsByMember, membersLookup, questionsLookup);
    }

    private static Dictionary<int, IEnumerable<DiscussionEventInfo>> FilterEventsForMembers(
        List<DiscussionMemberNotificationSettingsInfo> memberSettings,
        List<DiscussionEventInfo> notifiableEvents,
        DateTime limitDate,
        Dictionary<int, HashSet<int>> subscribedQuestionsByMemberID,
        Dictionary<int, QAndAQuestionPage> questionsLookup)
    {
        // Optimization: Group events by question to avoid iterating all events per member
        var eventsByQuestion = notifiableEvents
            .Where(e => questionsLookup.ContainsKey(e.DiscussionEventWebPageItemID))
            .GroupBy(e => e.DiscussionEventWebPageItemID)
            .ToDictionary(g => g.Key, g => g.ToList());

        var eventsByMember = new Dictionary<int, IEnumerable<DiscussionEventInfo>>();

        foreach (var settings in memberSettings)
        {
            int memberID = settings.DiscussionMemberNotificationSettingsMemberID;
            var memberSubscribedQuestions = subscribedQuestionsByMemberID.GetValueOrDefault(memberID, []);

            if (memberSubscribedQuestions.Count == 0)
            {
                continue;
            }

            var memberEvents = new List<DiscussionEventInfo>();

            // Only iterate events for questions the member is subscribed to
            foreach (int questionID in memberSubscribedQuestions)
            {
                if (!eventsByQuestion.TryGetValue(questionID, out var questionEvents))
                {
                    continue;
                }

                foreach (var evt in questionEvents)
                {
                    if (IsEventNewForMember(evt, settings, limitDate) && evt.DiscussionEventFromMemberID != memberID)
                    {
                        memberEvents.Add(evt);
                    }
                }
            }

            if (memberEvents.Count > 0)
            {
                eventsByMember[memberID] = memberEvents;
            }
        }

        return eventsByMember;
    }

    private static bool IsEventNewForMember(
        DiscussionEventInfo evt,
        DiscussionMemberNotificationSettingsInfo settings,
        DateTime limitDate) =>
        (settings.DiscussionMemberNotificationSettingsLatestDiscussionEventID != 0)
            ? (evt.DiscussionEventID > settings.DiscussionMemberNotificationSettingsLatestDiscussionEventID)
            : (evt.DiscussionEventDateModified > limitDate);

    private async Task<Dictionary<int, MemberInfo>> LoadMembersLookup(
        Dictionary<int, IEnumerable<DiscussionEventInfo>> eventsByMember,
        Dictionary<int, QAndAQuestionPage> questionsLookup)
    {
        var memberIDs = eventsByMember.Values
            .SelectMany(e => e)
            .Select(e => e.DiscussionEventFromMemberID)
            .Union(questionsLookup.Values.Select(x => x.QAndAQuestionPageAuthorMemberID))
            .Union(eventsByMember.Keys)
            .Distinct();

        return (await memberInfoProvider
            .Get()
            .WhereIn(nameof(MemberInfo.MemberID), memberIDs)
            .GetEnumerableTypedResultAsync())
            .ToDictionary(x => x.MemberID, x => x);
    }

    private async Task<List<QAndAMemberNotificationsBatch>> BuildNotificationBatches(
        Dictionary<int, IEnumerable<DiscussionEventInfo>> eventsByMember,
        Dictionary<int, MemberInfo> membersLookup,
        Dictionary<int, QAndAQuestionPage> questionsLookup)
    {
        var batches = new List<QAndAMemberNotificationsBatch>();

        foreach (var (memberID, eventsForMember) in eventsByMember)
        {
            if (!TryGetValidMember(memberID, membersLookup, out var communityMember))
            {
                continue;
            }

            int memberLastEventID = eventsForMember.Any()
                ? eventsForMember.Max(e => e.DiscussionEventID)
                : 0;

            batches.Add(new QAndAMemberNotificationsBatch
            {
                Member = communityMember,
                LastProcessedEventID = memberLastEventID,
                Notifications = await BuildNotifications(eventsForMember, membersLookup, questionsLookup)
            });
        }

        return batches;
    }

    private static bool TryGetValidMember(
        int memberID,
        Dictionary<int, MemberInfo> membersLookup,
        out CommunityMember communityMember)
    {
        communityMember = null!;

        if (!membersLookup.TryGetValue(memberID, out var member) || !member.MemberEnabled)
        {
            return false;
        }

        communityMember = member.AsCommunityMember();
        return communityMember.ModerationStatus == ModerationStatuses.None;
    }

    private async Task<IReadOnlyList<QAndANotification>> BuildNotifications(
        IEnumerable<DiscussionEventInfo> events,
        Dictionary<int, MemberInfo> membersLookup,
        Dictionary<int, QAndAQuestionPage> questionsLookup)
    {
        var notifications = new List<QAndANotification>(events.Count());

        foreach (var evt in events.OrderByDescending(e => e.DiscussionEventDateModified))
        {
            var page = questionsLookup[evt.DiscussionEventWebPageItemID];
            notifications.Add(new QAndANotification
            {
                AnswerAuthorName = membersLookup.TryGetValue(evt.DiscussionEventFromMemberID, out var eventByMember)
                ? eventByMember.MemberName
                : "",
                QuestionPage = page,
                QuestionPageURL = await webPageUrlRetriever.Retrieve(page),
                Event = Enum.Parse<QAndANotificationEventType>(evt.DiscussionEventType),
                EventDate = evt.DiscussionEventDateModified
            });
        }

        return notifications;
    }


    private async Task<List<DiscussionMemberNotificationSettingsInfo>> GetAllSettingsAtFrequency(QAndANotificationFrequencyType frequency) =>
        [.. await settingsProvider
            .Get()
            .WhereEquals(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsFrequencyType), Enum.GetName(frequency))
            .GetEnumerableTypedResultAsync()];

    private async Task<Dictionary<int, HashSet<int>>> GetSubscribedQuestionsGroupedByMember(
        List<DiscussionMemberNotificationSettingsInfo> memberSettings,
        int[] questionWebPageIDs)
    {
        var subscribedQuestions = await notificationsProvider
            .Get()
            .WhereIn(nameof(DiscussionMemberSubscriptionInfo.DiscussionMemberSubscriptionMemberID),
                memberSettings.Select(s => s.DiscussionMemberNotificationSettingsMemberID))
            .WhereIn(nameof(DiscussionMemberSubscriptionInfo.DiscussionMemberSubscriptionWebPageItemID),
                questionWebPageIDs)
            .GetEnumerableTypedResultAsync();

        return subscribedQuestions
            .GroupBy(n => n.DiscussionMemberSubscriptionMemberID)
            .ToDictionary(
                g => g.Key,
                g => g.Select(n => n.DiscussionMemberSubscriptionWebPageItemID).ToHashSet());
    }

    private async Task<Dictionary<int, QAndAQuestionPage>> GetQuestionsByWebPageItemID(int[] questionWebPageIDs) =>
        (await contentRetriever.RetrievePages<QAndAQuestionPage>(
            new RetrievePagesParameters
            {
                ChannelName = PortalWebSiteChannel.CODE_NAME,
                LanguageName = PortalWebSiteChannel.DEFAULT_LANGUAGE,
                IsForPreview = false
            },
            q => q.Where(w => w
                .WhereIn(
                    nameof(QAndAQuestionPage.SystemFields.WebPageItemID),
                    questionWebPageIDs)),
            RetrievalCacheSettings.CacheDisabled
        ))
        .ToDictionary(x => x.SystemFields.WebPageItemID, x => x);

    private async Task<(DateTime limitDate, List<DiscussionEventInfo> notifiableEvents)> GetNotifiableEvents(QAndANotificationFrequencyType frequency)
    {
        // Use time-based filter to avoid over-querying (performance optimization)
        // Per-member filtering by LastEventID happens in-memory via IsEventNewForMember
        var limitDate = MapFrequencyToDate(clock.GetUtcNow(), frequency);
        var notifiableEvents = (await eventProvider.Get()
            .WhereGreaterThan(nameof(DiscussionEventInfo.DiscussionEventDateModified), limitDate)
            .OrderBy(OrderDirection.Ascending, nameof(DiscussionEventInfo.DiscussionEventDateModified))
            .GetEnumerableTypedResultAsync())
            .ToList();
        return (limitDate, notifiableEvents);
    }

    private async Task<Result> UpdateMemberNotificationSettings(CommunityMember member, int lastProcessedEventID, QAndANotificationFrequencyType frequency)
    {
        // Filter by frequency for query precision (though member can only have one frequency setting)
        var settings = (await settingsProvider
            .Get()
            .WhereEquals(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsMemberID), member.Id)
            .WhereEquals(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsFrequencyType), Enum.GetName(frequency))
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (settings is null)
        {
            return Result.Failure($"Missing Q&A notification settings for member [{member.Id}]");
        }

        settings.DiscussionMemberNotificationSettingsLatestDiscussionEventID = lastProcessedEventID;
        await settingsProvider.SetAsync(settings);

        return Result.Success();
    }

    // NOTE: Converts DateTimeOffset to DateTime, losing timezone info. This assumes server timezone remains constant.
    // If server timezone changes, comparisons with DiscussionEventDateModified may have subtle bugs.
    private static DateTime MapFrequencyToDate(DateTimeOffset now, QAndANotificationFrequencyType frequency) =>
        frequency switch
        {
            QAndANotificationFrequencyType.Weekly => now.DateTime.AddDays(-7),
            QAndANotificationFrequencyType.DailyOnce => now.DateTime.AddDays(-1),
            QAndANotificationFrequencyType.DailyTwice => now.DateTime.AddHours(-12),
            QAndANotificationFrequencyType.Monthly => now.DateTime.AddMonths(-1),
            QAndANotificationFrequencyType.Never => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        };

    private async Task<string> GetBaseUrl()
    {
        var emailChannel = (await emailChannelProvider.Get()
            .Source(s => s.Join<ChannelInfo>(nameof(EmailChannelInfo.EmailChannelChannelID), nameof(ChannelInfo.ChannelID)))
            .WhereEquals(nameof(ChannelInfo.ChannelName), SystemChannels.Emails.ChannelName)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        var emailChannelDomains = emailChannel is not null
            ? await emailChannelDomainProvider.GetDomains(emailChannel.EmailChannelID)
            : null;
        string domain = emailChannelDomains?.ServiceDomain ?? "community.kentico.com";
        return $"https://{domain}";
    }
}

public enum QAndANotificationFrequencyType
{
    [Description("Never")]
    Never,
    [Description("Twice Daily")]
    DailyTwice,
    [Description("Daily")]
    DailyOnce,
    [Description("Weekly")]
    Weekly,
    [Description("Monthly")]
    Monthly
}

public enum QAndANotificationEventType
{
    [Description("Response created")]
    ResponseCreated,
    [Description("Answer accepted")]
    AnswerAccepted
}

public class QAndAMemberNotificationsBatch
{
    public required CommunityMember Member { get; set; }
    public IReadOnlyList<QAndANotification> Notifications { get; set; } = [];
    public int LastProcessedEventID { get; set; }
}

public class QAndANotification
{
    public required QAndANotificationEventType Event { get; init; }
    public required QAndAQuestionPage QuestionPage { get; set; }
    public required WebPageUrl QuestionPageURL { get; set; }
    public required string AnswerAuthorName { get; set; }
    public DateTime EventDate { get; set; }
}
