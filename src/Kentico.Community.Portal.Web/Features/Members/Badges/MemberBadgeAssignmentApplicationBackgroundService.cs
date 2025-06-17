using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;

using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class MemberBadgeAssignmentApplicationBackgroundService : ApplicationBackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMemberBadgeInfoProvider badgeInfoProvider;
    private readonly IMemberBadgeMemberInfoProvider badgeMemberInfoProvider;
    private readonly IProgressiveCache cache;
    private readonly IInfoProvider<MemberInfo> memberInfoProvider;
    private readonly IInfoProvider<MemberBadgeConfigurationInfo> memberBadgeConfigurationProvider;
    private readonly IEventLogService log;
    private readonly TimeProvider clock;

    private static int RestartDelayMinutes { get; } = 10;

    public MemberBadgeAssignmentApplicationBackgroundService(
        IServiceProvider serviceProvider,
        IMemberBadgeInfoProvider badgeInfoProvider,
        IMemberBadgeMemberInfoProvider badgeMemberInfoProvider,
        IProgressiveCache cache,
        IInfoProvider<MemberInfo> memberInfoProvider,
        IInfoProvider<MemberBadgeConfigurationInfo> memberBadgeConfigurationProvider,
        IEventLogService log,
        TimeProvider clock)
    {
        this.serviceProvider = serviceProvider;
        this.badgeInfoProvider = badgeInfoProvider;
        this.badgeMemberInfoProvider = badgeMemberInfoProvider;
        this.cache = cache;
        this.memberInfoProvider = memberInfoProvider;
        this.memberBadgeConfigurationProvider = memberBadgeConfigurationProvider;
        this.log = log;
        this.clock = clock;

        ShouldRestart = true;
        RestartDelay = TimeSpan.FromMinutes(RestartDelayMinutes).Milliseconds;
    }

    /// <summary>
    /// Executes outer process loop which uses <see cref="MemberBadgeConfigurationInfo"/> to set the loop execution time
    /// </summary>
    /// <remarks>
    /// If <see cref="MemberBadgeConfigurationInfo.MemberBadgeConfigurationReExecuteLoopDelayMinutes"/> is updated, the new value will not apply until the time period of the old value has elapsed
    /// </remarks>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteInternal(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var configs = await memberBadgeConfigurationProvider.Get()
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: stoppingToken);
            if (configs.FirstOrDefault() is not MemberBadgeConfigurationInfo config)
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

                continue;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(config.MemberBadgeConfigurationReExecuteLoopDelayMinutes));

            await LoopInternal(config, timer, stoppingToken);
        }
    }

    /// <summary>
    /// Inner loop which controls badge assignment for 1 iteration of assignments
    /// </summary>
    /// <remarks>
    /// Each iteration will take at least the length of <paramref name="iterationTimer"/> to execute
    /// </remarks>
    /// <param name="config"></param>
    /// <param name="iterationTimer"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    private async Task LoopInternal(MemberBadgeConfigurationInfo config, PeriodicTimer iterationTimer, CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var assignmentRules = scope.ServiceProvider.GetServices<IMemberBadgeAssignmentRule>();

        if (config.MemberBadgeConfigurationIsLoggingVerbose)
        {
            log.LogInformation("Member Badge Assignment", "EXECUTE", $"Running badge assignment for {assignmentRules.Count()} rules");
        }

        var assignments = new Dictionary<string, string>();

        foreach (var assignmentRule in assignmentRules)
        {
            var result = await AssignBadges(assignmentRule, stoppingToken);

            if (config.MemberBadgeConfigurationIsLoggingVerbose)
            {
                assignments.Add(assignmentRule.BadgeCodeName, result.Match(count => count.ToString(), err => err));
            }

            await Task.Delay(TimeSpan.FromSeconds(config.MemberBadgeConfigurationNextRuleProcessDelaySeconds), stoppingToken);
        }

        if (config.MemberBadgeConfigurationIsLoggingVerbose)
        {
            string formattedMessage = string.Join(Environment.NewLine, assignments.Select(pair => $"{pair.Key}: {pair.Value}"));

            log.LogInformation(
                "Member Badge Assignment",
                "BADGE_ASSIGNMENTS",
                formattedMessage);
        }

        // Once all assignments have executed, wait for the timer to expire and then returns control to the caller
        if (await iterationTimer.WaitForNextTickAsync(stoppingToken))
        {
            return;
        }
    }

    /// <summary>
    /// Assigns badges for the given rule
    /// </summary>
    /// <param name="assignmentRule"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    private async Task<Result<int>> AssignBadges(IMemberBadgeAssignmentRule assignmentRule, CancellationToken stoppingToken)
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
            var assignments = await assignmentRule.Assign(matchingBadge, badgeMembers, members, stoppingToken);

            var infos = assignments
                .Select(a => new MemberBadgeMemberInfo
                {
                    MemberBadgeMemberMemberId = a.MemberID,
                    MemberBadgeMemberMemberBadgeId = a.MemberBadgeID,
                    MemberBadgeMemberIsSelected = false,
                    MemberBadgeMemberCreatedDate = clock.GetUtcNow().DateTime
                });

            if (infos.Any())
            {
                badgeMemberInfoProvider.BulkInsert(infos);

                CacheHelper.TouchKey($"{MemberBadgeMemberInfo.OBJECT_TYPE}|all");

                return infos.Count();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogException("Member Badge Assignment", "BADGE_ASSIGNMENT_FAILURE", ex);
            return Result.Failure<int>("ASSIGNMENT_EXCEPTION");
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
        }, new CacheSettings(60, nameof(GetEnabledMembersCached)));

        return members;
    }
}
