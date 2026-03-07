
using System.ComponentModel;
using EnumsNET;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport.Operations;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport;

public class TargetReportsViewModel : BaseWidgetViewModel
{
    private TargetReportsViewModel(
        bool isAuthenticated,
        int year,
        ProgramStatuses programStatus,
        string heading,
        IReadOnlyList<CommunityProgramTargetsReportProgress> goals,
        IReadOnlyList<CommunityProgramTargetsReportBadgeRequirement> badgeRequirements,
        string? errorMessage,
        Maybe<int> emulatedMemberId,
        IReadOnlyList<MemberSummary> availableMembers,
        TargetsReportWidgetProperties properties,
        bool isInternalEmployee = false,
        int currentAuthMemberId = 0)
    {
        IsAuthenticated = isAuthenticated;
        Year = year;
        ProgramStatus = programStatus;
        Heading = heading;
        Goals = goals;
        BadgeRequirements = badgeRequirements;
        ErrorMessage = errorMessage;
        IsInternalEmployee = isInternalEmployee;
        CurrentAuthMemberId = currentAuthMemberId;
        EmulatedMemberID = emulatedMemberId;
        AvailableMembers = availableMembers ?? [];
        Properties = properties;
    }

    protected override string WidgetName { get; } = TargetsReportWidget.NAME;

    public bool IsAuthenticated { get; }
    public int Year { get; }
    public ProgramStatuses ProgramStatus { get; }
    public string ProgramStatusDisplay => GetEnumDescription(ProgramStatus);

    public string? ErrorMessage { get; }

    public string Heading { get; }

    public IReadOnlyList<CommunityProgramTargetsReportProgress> Goals { get; }
    public IReadOnlyList<CommunityProgramTargetsReportBadgeRequirement> BadgeRequirements { get; }

    public bool IsInternalEmployee { get; }
    public int CurrentAuthMemberId { get; }
    public Maybe<int> EmulatedMemberID { get; }
    public IReadOnlyList<MemberSummary> AvailableMembers { get; }
    public TargetsReportWidgetProperties Properties { get; }
    public bool IsEmulating => EmulatedMemberID.HasValue;

    public static TargetReportsViewModel Error(TargetsReportWidgetProperties props, string message, int year) =>
        new(
            isAuthenticated: false,
            year: year,
            programStatus: ProgramStatuses.None,
            heading: props.Heading,
            goals: [],
            badgeRequirements: [],
            errorMessage: message,
            emulatedMemberId: Maybe<int>.None,
            availableMembers: [],
            properties: props);

    public static TargetReportsViewModel Create(
        TargetsReportWidgetProperties props,
        TargetsReportQueryResponse resp,
        Maybe<int> emulatedMemberId,
        IReadOnlyList<MemberSummary> availableMembers,
        bool isInternalEmployee = false,
        int currentAuthMemberId = 0)
    {
        var (goals, badges) = CreateGoals(props, resp);

        return new(
            isAuthenticated: true,
            year: resp.Year,
            programStatus: resp.ProgramStatus,
            heading: props.Heading,
            goals: goals,
            badgeRequirements: badges,
            errorMessage: null,
            isInternalEmployee: isInternalEmployee,
            currentAuthMemberId: currentAuthMemberId,
            emulatedMemberId: emulatedMemberId,
            availableMembers: availableMembers,
            properties: props);
    }

    private static (IReadOnlyList<CommunityProgramTargetsReportProgress> goals, IReadOnlyList<CommunityProgramTargetsReportBadgeRequirement> badges) CreateGoals(
        TargetsReportWidgetProperties props,
        TargetsReportQueryResponse resp)
    {
        if (resp.ProgramStatus == ProgramStatuses.MVP)
        {
            var goals = new List<CommunityProgramTargetsReportProgress>
            {
                new("Activities (year)", resp.ActivitiesSubmittedCountYear, props.MvpActivitiesTargetYear),
                new("Q&A Discussions created (year)", resp.DiscussionsCreatedCountYear, props.MvpDiscussionsCreatedTargetYear),
                new("Kentico feedback activities (year)", resp.KenticoFeedbackActivitiesCountYear, props.MvpKenticoFeedbackActivitiesTargetYear)
            };

            goals.AddRange(CreateQuarterGoals("Social posts", resp.SocialPostsCountByQuarter, props.MvpSocialPostsTargetQuarter));
            goals.AddRange(CreateQuarterGoals("Activities", resp.ActivitiesSubmittedCountByQuarter, props.MvpTotalActivitiesTargetQuarter));

            var badges = new List<CommunityProgramTargetsReportBadgeRequirement>
            {
                new("Sales certification", resp.HasSalesCertificationBadge),
                new("Developer or marketer certification", resp.HasDeveloperOrMarketerCertificationBadge)
            };

            return (goals, badges);
        }

        if (resp.ProgramStatus == ProgramStatuses.CommunityLeader)
        {
            var goals = new List<CommunityProgramTargetsReportProgress>
            {
                new("Activities (year)", resp.ActivitiesSubmittedCountYear, props.CommunityLeaderActivitiesTargetYear),
                new("Q&A Discussions created (year)", resp.DiscussionsCreatedCountYear, props.CommunityLeaderDiscussionsCreatedTargetYear),
                new("Kentico feedback activities (year)", resp.KenticoFeedbackActivitiesCountYear, props.CommunityLeaderKenticoFeedbackActivitiesTargetYear)
            };

            goals.AddRange(CreateQuarterGoals("Social posts", resp.SocialPostsCountByQuarter, props.CommunityLeaderSocialPostsTargetQuarter));
            goals.AddRange(CreateQuarterGoals("Activities", resp.ActivitiesSubmittedCountByQuarter, props.CommunityLeaderTotalActivitiesTargetQuarter));

            var badges = new List<CommunityProgramTargetsReportBadgeRequirement>
            {
                new("Content modeling certification", resp.HasContentModelingCertificationBadge),
                new("Developer or marketer certification", resp.HasDeveloperOrMarketerCertificationBadge)
            };

            return (goals, badges);
        }

        var fallbackGoals = new List<CommunityProgramTargetsReportProgress>
        {
            new("Activities (year)", resp.ActivitiesSubmittedCountYear, 0),
            new("Discussions created (year)", resp.DiscussionsCreatedCountYear, 0),
            new("Kentico feedback activities (year)", resp.KenticoFeedbackActivitiesCountYear, 0)
        };

        fallbackGoals.AddRange(CreateQuarterGoals("Social posts", resp.SocialPostsCountByQuarter, 0));
        fallbackGoals.AddRange(CreateQuarterGoals("Activities", resp.ActivitiesSubmittedCountByQuarter, 0));

        return (fallbackGoals, []);
    }

    private static List<CommunityProgramTargetsReportProgress> CreateQuarterGoals(
        string label,
        IReadOnlyList<int> countsByQuarter,
        int targetPerQuarter)
    {
        var goals = new List<CommunityProgramTargetsReportProgress>(capacity: 4);

        for (int i = 0; i < 4; i++)
        {
            string quarterLabel = $"{label} (Q{i + 1})";
            int current = i < countsByQuarter.Count ? countsByQuarter[i] : 0;
            goals.Add(new CommunityProgramTargetsReportProgress(quarterLabel, current, targetPerQuarter));
        }

        return goals;
    }

    private static string GetEnumDescription<T>(T value)
        where T : struct, Enum
    {
        var member = Enums.GetMember(value);
        if (member is null)
        {
            return value.ToString();
        }

        return member.Attributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? member.Name;
    }

    public bool IsQuarterly(string label) => label.Contains("(Q", StringComparison.OrdinalIgnoreCase);

    public string GetQuarterGroupKey(string label)
    {
        int idx = label.LastIndexOf(" (Q", StringComparison.OrdinalIgnoreCase);
        return idx > 0 ? label[..idx] : label;
    }

    public int? GetQuarterIndex(string label)
    {
        int idx = label.LastIndexOf(" (Q", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        string part = label[(idx + 3)..].TrimEnd(')', ' ');
        return int.TryParse(part, out int q) ? q : null;
    }
}

public record CommunityProgramTargetsReportProgress(string Label, int Current, int Target)
{
    public bool? IsConfiguredOverride { get; init; }

    public bool IsConfigured => IsConfiguredOverride ?? (Target > 0);

    public decimal Ratio => Target <= 0 ? 0 : (decimal)Current / Target;

    public int Percent => Target <= 0
        ? 0
        : (int)Math.Clamp(Math.Round(Ratio * 100m, 0), 0, 100);

    public static CommunityProgramTargetsReportProgress Empty(string label) => new(label, 0, 0);
}

public record CommunityProgramTargetsReportBadgeRequirement(string Label, bool IsSatisfied);
