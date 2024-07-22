using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class MemberBadgeAssignmentApplicationBackgroundService : ApplicationBackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMemberBadgeInfoProvider badgeInfoProvider;
    private readonly IMemberBadgeMemberInfoProvider badgeMemberInfoProvider;
    private readonly IProgressiveCache cache;
    private readonly IInfoProvider<MemberInfo> memberInfoProvider;
    private readonly IEventLogService log;
    private readonly ISystemClock clock;

    private static int RestartDelayMinutes { get; } = 10;
    private static int ReExecuteLoopDelayMinutes { get; } = 60;
    private static int NextRuleProcessDelaySeconds { get; } = 60;

    public MemberBadgeAssignmentApplicationBackgroundService(
        IServiceProvider serviceProvider,
        IMemberBadgeInfoProvider badgeInfoProvider,
        IMemberBadgeMemberInfoProvider badgeMemberInfoProvider,
        IProgressiveCache cache,
        IInfoProvider<MemberInfo> memberInfoProvider,
        IEventLogService log,
        ISystemClock clock)
    {
        this.serviceProvider = serviceProvider;
        this.badgeInfoProvider = badgeInfoProvider;
        this.badgeMemberInfoProvider = badgeMemberInfoProvider;
        this.cache = cache;
        this.memberInfoProvider = memberInfoProvider;
        this.log = log;
        this.clock = clock;
        ShouldRestart = true;
        RestartDelay = TimeSpan.FromMinutes(RestartDelayMinutes).Milliseconds;
    }

    protected override async Task ExecuteInternal(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(ReExecuteLoopDelayMinutes));

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            var assignmentRules = serviceProvider.GetServices<IMemberBadgeAssignmentRule>();

            foreach (var assignmentRule in assignmentRules)
            {
                var badges = await badgeInfoProvider.GetAllMemberBadgesCached();
                var badgeMembers = await badgeMemberInfoProvider.GetAllMemberBadgeRelationshipsCached();
                var members = await GetEnabledMembersCached();

                var matchingBadge = badges.FirstOrDefault(b => string.Equals(b.MemberBadgeCodeName, assignmentRule.BadgeCodeName, StringComparison.OrdinalIgnoreCase));

                if (matchingBadge is null || !matchingBadge.MemberBadgeIsEnabledForRuleAssignment)
                {
                    continue;
                }

                try
                {
                    var assignments = await assignmentRule.Assign(matchingBadge, badgeMembers, members, stoppingToken);

                    if (assignments.Any())
                    {
                        var infos = assignments.Select(a => new MemberBadgeMemberInfo
                        {
                            MemberBadgeMemberMemberId = a.MemberID,
                            MemberBadgeMemberMemberBadgeId = a.MemberBadgeID,
                            MemberBadgeMemberIsSelected = false,
                            MemberBadgeMemberCreatedDate = clock.UtcNow
                        });

                        badgeMemberInfoProvider.BulkInsert(infos);

                        CacheHelper.TouchKey($"{MemberBadgeMemberInfo.OBJECT_TYPE}|all");

                        log.LogInformation("Member Badge Assignment", "NEW_BADGE_ASSIGNMENTS", $"{assignments.Count} new badges have been assigned for badge [{assignmentRule.BadgeCodeName}]");
                    }
                }
                catch (Exception ex)
                {
                    log.LogException("Member Badge Assignment", "BADGE_ASSIGNMENT_FAILURE", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(NextRuleProcessDelaySeconds), stoppingToken);
            }
        }
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
        }, new CacheSettings(60, $"{MemberInfo.OBJECT_TYPE}|all"));

        return members;
    }
}
