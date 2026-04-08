using System.Collections.Immutable;
using Kentico.Xperience.Admin.Base;

namespace Kentico.Community.Portal.Admin.Features.Reporting;

public class MembershipStatsLayoutClientProperties : TemplateClientProperties
{
    public MembershipStatsDashboard Stats { get; set; } = null!;
    public string Title { get; set; } = "Membership stats";
    public string Description { get; set; } = "Review moderation volume, email-domain spread, and member contribution leaders.";
}

public record MembershipStatsDashboard(
    MembershipStatsOverview Overview,
    ImmutableList<MemberModerationStatusCount> ModerationStatuses,
    ImmutableList<MemberEmailDomainCount> EnabledEmailDomains,
    ImmutableList<MemberEmailDomainCount> ModeratedEmailDomains,
    ImmutableList<MemberContributionLeaderboard> Leaderboards);

public record MembershipStatsOverview(
    int EnabledMembers,
    int ModeratedMembers,
    int UniqueEnabledEmailDomains,
    int UniqueModeratedEmailDomains);

public record MemberModerationStatusCount(string Key, string Label, int Value);

public record MemberEmailDomainCount(string Domain, int Count);

public record MemberContributionLeaderboard(
    string Key,
    string Label,
    ImmutableList<MemberContributionLeader> Members);

public record MemberContributionLeader(
    int MemberId,
    string DisplayName,
    string Email,
    int Count,
    string EditUrl,
    string ViewUrl);
