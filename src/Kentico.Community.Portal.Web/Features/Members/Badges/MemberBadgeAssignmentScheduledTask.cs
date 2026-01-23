using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.Scheduler;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Members.Badges;

[assembly: RegisterScheduledTask(
    identifier: MemberBadgeAssignmentScheduledTask.IDENTIFIER,
    taskType: typeof(MemberBadgeAssignmentScheduledTask))]

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public partial class MemberBadgeAssignmentScheduledTask(
    IServiceProvider serviceProvider,
    IMemberBadgeInfoProvider badgeInfoProvider,
    IMemberBadgeMemberInfoProvider badgeMemberInfoProvider,
    IProgressiveCache cache,
    IInfoProvider<MemberInfo> memberInfoProvider,
    IInfoProvider<MemberBadgeConfigurationInfo> memberBadgeConfigurationProvider,
    ILogger<MemberBadgeAssignmentScheduledTask> log,
    TimeProvider clock) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.MemberBadgeAssignmentScheduledTask";

    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly IMemberBadgeInfoProvider badgeInfoProvider = badgeInfoProvider;
    private readonly IMemberBadgeMemberInfoProvider badgeMemberInfoProvider = badgeMemberInfoProvider;
    private readonly IProgressiveCache cache = cache;
    private readonly IInfoProvider<MemberInfo> memberInfoProvider = memberInfoProvider;
    private readonly IInfoProvider<MemberBadgeConfigurationInfo> memberBadgeConfigurationProvider = memberBadgeConfigurationProvider;
    private readonly TimeProvider clock = clock;


    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Member Badge Assignment: No configuration found.")]
    partial void LogNoConfiguration();

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Member Badge Assignment: Running badge assignment for {Count} rules")]
    partial void LogBadgeAssignmentStart(int count);

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Member Badge Assignment: {Assignments}")]
    partial void LogBadgeAssignmentsEnd(string assignments);

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Member Badge Assignment")]
    partial void LogBadgeAssignmentFailure(Exception ex);

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        var configs = await memberBadgeConfigurationProvider.Get()
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
        if (configs.FirstOrDefault() is not MemberBadgeConfigurationInfo config)
        {
            LogNoConfiguration();
            return new ScheduledTaskExecutionResult("No configuration found.");
        }

        using var scope = serviceProvider.CreateScope();
        var assignmentRules = scope.ServiceProvider.GetServices<IMemberBadgeAssignmentRule>();
        var assignments = new Dictionary<string, string>();

        if (config.MemberBadgeConfigurationIsLoggingVerbose)
        {
            LogBadgeAssignmentStart(assignmentRules.Count());
        }

        foreach (var assignmentRule in assignmentRules)
        {
            var result = await AssignBadges(assignmentRule, cancellationToken);
            if (config.MemberBadgeConfigurationIsLoggingVerbose)
            {
                assignments.TryAdd(assignmentRule.BadgeCodeName, result.Match(count => count.ToString(), err => err));
            }
            await Task.Delay(TimeSpan.FromSeconds(config.MemberBadgeConfigurationNextRuleProcessDelaySeconds), cancellationToken);
        }

        if (config.MemberBadgeConfigurationIsLoggingVerbose)
        {
            string assignmentsText = string.Join(", ", assignments.Select(pair => $"{pair.Key}: {pair.Value}"));
            LogBadgeAssignmentsEnd(assignmentsText);
        }

        return ScheduledTaskExecutionResult.Success;
    }

    private async Task<Result<int>> AssignBadges(IMemberBadgeAssignmentRule assignmentRule, CancellationToken cancellationToken)
    {
        var badges = await badgeInfoProvider.GetAllMemberBadgesCached();
        var badgeMembers = await badgeMemberInfoProvider.GetAllMemberBadgeRelationshipsCached();
        var members = await GetEnabledMembersCached();

        var matchingBadge = badges.FirstOrDefault(b => string.Equals(b.MemberBadgeCodeName, assignmentRule.BadgeCodeName, StringComparison.OrdinalIgnoreCase));

        if (matchingBadge is null)
        {
            return Result.Failure<int>("NO_MATCHING_BADGE");
        }

        if (!matchingBadge.MemberBadgeIsEnabledForRuleAssignment)
        {
            return Result.Failure<int>("BADGE_ASSIGNMENT_DISABLED");
        }

        try
        {
            var assignments = await assignmentRule.Assign(matchingBadge, badgeMembers, members, cancellationToken);

            var infos = assignments
                .Select(a => new MemberBadgeMemberInfo
                {
                    MemberBadgeMemberMemberId = a.MemberID,
                    MemberBadgeMemberMemberBadgeId = a.MemberBadgeID,
                    MemberBadgeMemberIsSelected = false,
                    MemberBadgeMemberCreatedDate = clock.GetUtcNow().DateTime
                })
                .ToList();

            if (infos.Count != 0)
            {
                badgeMemberInfoProvider.BulkInsert(infos);
                CacheHelper.TouchKey($"{MemberBadgeMemberInfo.OBJECT_TYPE}|all");
                return infos.Count;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogBadgeAssignmentFailure(ex);
            return Result.Failure<int>("ASSIGNMENT_EXCEPTION. See Logs.");
        }

        return 0;
    }

    private async Task<IReadOnlyList<MemberInfo>> GetEnabledMembersCached()
    {
        var members = await cache.LoadAsync(async cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberInfo.OBJECT_TYPE}|all");
            var m = await memberInfoProvider.Get()
                .WhereTrue(nameof(MemberInfo.MemberEnabled))
                .GetEnumerableTypedResultAsync();
            return m.ToList();
        }, new CacheSettings(cacheMinutes: 60, nameof(GetEnabledMembersCached)));
        return members;
    }
}
