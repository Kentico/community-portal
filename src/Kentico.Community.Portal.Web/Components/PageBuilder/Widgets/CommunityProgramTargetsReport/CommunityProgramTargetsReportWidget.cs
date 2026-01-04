using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using CMS.OnlineForms;
using CMS.Websites.Routing;
using EnumsNET;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CommunityProgramTargetsReportWidget.IDENTIFIER,
    name: CommunityProgramTargetsReportWidget.NAME,
    viewComponentType: typeof(CommunityProgramTargetsReportWidget),
    propertiesType: typeof(CommunityProgramTargetsReportWidgetProperties),
    Description = "Shows progress towards annual community program targets for the current member",
    IconClass = KenticoIcons.CHECKLIST)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport;

public class CommunityProgramTargetsReportWidget(
    IMediator mediator,
    IWebsiteChannelContext channelContext,
    TimeProvider timeProvider,
    ILogger<CommunityProgramTargetsReportWidget> logger) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.CommunityProgramTargetsReport";
    public const string NAME = "Community program targets report";

    private readonly IMediator mediator = mediator;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger<CommunityProgramTargetsReportWidget> logger = logger;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<CommunityProgramTargetsReportWidgetProperties> component)
    {
        int memberId = CommunityMember.GetMemberIDFromClaim(HttpContext).Value;
        int year = timeProvider.GetLocalNow().Year;

        if (memberId <= 0)
        {
            return View(
                "~/Components/PageBuilder/Widgets/CommunityProgramTargetsReport/CommunityProgramTargetsReport.cshtml",
                CommunityProgramTargetsReportWidgetViewModel.Error(component.Properties, "This content is only visible to authenticated members.", year));
        }

        try
        {
            var resp = await mediator.Send(new CommunityProgramTargetsReportQuery(
                MemberId: memberId,
                ChannelName: channelContext.WebsiteChannelName,
                Year: year), HttpContext.RequestAborted);

            return View(
                "~/Components/PageBuilder/Widgets/CommunityProgramTargetsReport/CommunityProgramTargetsReport.cshtml",
                CommunityProgramTargetsReportWidgetViewModel.Create(component.Properties, resp));
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to render community program targets report for member {MemberId}", memberId);

            return View(
                "~/Components/PageBuilder/Widgets/CommunityProgramTargetsReport/CommunityProgramTargetsReport.cshtml",
                CommunityProgramTargetsReportWidgetViewModel.Error(component.Properties, "Unable to load program targets right now.", year));
        }
    }

}

public class CommunityProgramTargetsReportWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Heading",
        ExplanationText = "Optional widget heading",
        Order = 1)]
    public string Heading { get; set; } = "";

    [NumberInputComponent(Label = "MVP: annual activities target", Order = 10)]
    [Range(0, 10000)]
    public int MvpActivitiesTargetYear { get; set; } = 30;

    [NumberInputComponent(Label = "MVP: annual discussions created target", Order = 11)]
    [Range(0, 10000)]
    public int MvpDiscussionsCreatedTargetYear { get; set; } = 6;

    [NumberInputComponent(Label = "MVP: quarterly social posts target", Order = 12)]
    [Range(0, 10000)]
    public int MvpSocialPostsTargetQuarter { get; set; } = 4;

    [NumberInputComponent(Label = "MVP: quarterly total activities target", Order = 13)]
    [Range(0, 10000)]
    public int MvpTotalActivitiesTargetQuarter { get; set; } = 6;

    [NumberInputComponent(Label = "MVP: annual Kentico feedback activities target", Order = 14)]
    [Range(0, 10000)]
    public int MvpKenticoFeedbackActivitiesTargetYear { get; set; } = 8;

    [NumberInputComponent(Label = "Community Leader: annual activities target", Order = 20)]
    [Range(0, 10000)]
    public int CommunityLeaderActivitiesTargetYear { get; set; } = 30;

    [NumberInputComponent(Label = "Community Leader: annual discussions created target", Order = 21)]
    [Range(0, 10000)]
    public int CommunityLeaderDiscussionsCreatedTargetYear { get; set; } = 12;

    [NumberInputComponent(Label = "Community Leader: quarterly social posts target", Order = 22)]
    [Range(0, 10000)]
    public int CommunityLeaderSocialPostsTargetQuarter { get; set; } = 3;

    [NumberInputComponent(Label = "Community Leader: quarterly total activities target", Order = 23)]
    [Range(0, 10000)]
    public int CommunityLeaderTotalActivitiesTargetQuarter { get; set; } = 4;

    [NumberInputComponent(Label = "Community Leader: annual Kentico feedback activities target", Order = 24)]
    [Range(0, 10000)]
    public int CommunityLeaderKenticoFeedbackActivitiesTargetYear { get; set; } = 4;
}

public class CommunityProgramTargetsReportWidgetViewModel : BaseWidgetViewModel
{
    private CommunityProgramTargetsReportWidgetViewModel(
        bool isAuthenticated,
        int year,
        ProgramStatuses programStatus,
        string heading,
        IReadOnlyList<CommunityProgramTargetsReportProgress> goals,
        IReadOnlyList<CommunityProgramTargetsReportBadgeRequirement> badgeRequirements,
        string? errorMessage)
    {
        IsAuthenticated = isAuthenticated;
        Year = year;
        ProgramStatus = programStatus;
        Heading = heading;
        Goals = goals;
        BadgeRequirements = badgeRequirements;
        ErrorMessage = errorMessage;
    }

    protected override string WidgetName { get; } = CommunityProgramTargetsReportWidget.NAME;

    public bool IsAuthenticated { get; }
    public int Year { get; }
    public ProgramStatuses ProgramStatus { get; }
    public string ProgramStatusDisplay => GetEnumDescription(ProgramStatus);

    public string? ErrorMessage { get; }

    public string Heading { get; }

    public IReadOnlyList<CommunityProgramTargetsReportProgress> Goals { get; }
    public IReadOnlyList<CommunityProgramTargetsReportBadgeRequirement> BadgeRequirements { get; }

    public static CommunityProgramTargetsReportWidgetViewModel Error(CommunityProgramTargetsReportWidgetProperties props, string message, int year) =>
        new(
            isAuthenticated: false,
            year: year,
            programStatus: ProgramStatuses.None,
            heading: props.Heading,
            goals: [],
            badgeRequirements: [],
            errorMessage: message);

    public static CommunityProgramTargetsReportWidgetViewModel Create(CommunityProgramTargetsReportWidgetProperties props, CommunityProgramTargetsReportQueryResponse resp)
    {
        var (goals, badges) = CreateGoals(props, resp);

        return new(
            isAuthenticated: true,
            year: resp.Year,
            programStatus: resp.ProgramStatus,
            heading: props.Heading,
            goals: goals,
            badgeRequirements: badges,
            errorMessage: null);
    }

    private static (IReadOnlyList<CommunityProgramTargetsReportProgress> goals, IReadOnlyList<CommunityProgramTargetsReportBadgeRequirement> badges) CreateGoals(
        CommunityProgramTargetsReportWidgetProperties props,
        CommunityProgramTargetsReportQueryResponse resp)
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

    private static IReadOnlyList<CommunityProgramTargetsReportProgress> CreateQuarterGoals(
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

public record CommunityProgramTargetsReportQuery(
    int MemberId,
    string ChannelName,
    int Year)
    : IQuery<CommunityProgramTargetsReportQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => $"{ChannelName}|{MemberId}|{Year}";
}

public record CommunityProgramTargetsReportQueryResponse(
    int MemberId,
    int Year,
    ProgramStatuses ProgramStatus,
    int ActivitiesSubmittedCountYear,
    IReadOnlyList<int> ActivitiesSubmittedCountByQuarter,
    IReadOnlyList<int> SocialPostsCountByQuarter,
    int KenticoFeedbackActivitiesCountYear,
    int DiscussionsCreatedCountYear,
    int BadgesAssignedCount,
    bool HasSalesCertificationBadge,
    bool HasContentModelingCertificationBadge,
    bool HasDeveloperOrMarketerCertificationBadge,
    int AuthoredQuestionsCountYear);

public class CommunityProgramTargetsReportQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<MemberInfo> memberProvider,
    MemberBadgeService memberBadgeService,
    IContentQueryExecutor executor) : DataItemQueryHandler<CommunityProgramTargetsReportQuery, CommunityProgramTargetsReportQueryResponse>(tools)
{
    private const string SocialPostActivityType = "Social media promotion";

    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly MemberBadgeService memberBadgeService = memberBadgeService;
    private readonly IContentQueryExecutor executor = executor;

    public override async Task<CommunityProgramTargetsReportQueryResponse> Handle(CommunityProgramTargetsReportQuery request, CancellationToken cancellationToken = default)
    {
        var yearStart = new DateTime(request.Year, 1, 1);

        var member = await memberProvider.Get()
            .WhereEquals(nameof(MemberInfo.MemberID), request.MemberId)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var programStatus = member.FirstOrDefault()?.AsCommunityMember().ProgramStatus ?? ProgramStatuses.None;

        var activityItemsYear = await BizFormItemProvider.GetItems<CommunityProgramActivity_2026Item>()
            .WhereEquals(nameof(CommunityProgramActivity_2026Item.MemberID), request.MemberId)
            .WhereGreaterOrEquals(nameof(CommunityProgramActivity_2026Item.ActivityDate), yearStart)
            .GetEnumerableTypedResultAsync();

        int activitiesSubmittedCountYear = activityItemsYear.Count();

        int[] activitiesSubmittedCountByQuarter = new int[4];
        int[] socialPostsCountByQuarter = new int[4];

        foreach (var item in activityItemsYear)
        {
            int quarterIndex = GetQuarterIndex(item.ActivityDate);
            if (quarterIndex is < 0 or > 3)
            {
                continue;
            }

            activitiesSubmittedCountByQuarter[quarterIndex]++;

            if (string.Equals(item.ActivityType, SocialPostActivityType, StringComparison.OrdinalIgnoreCase))
            {
                socialPostsCountByQuarter[quarterIndex]++;
            }
        }

        int kenticoFeedbackActivitiesCountYear = activityItemsYear
            .Count(i => i.ActivityType.StartsWith("Feedback:", StringComparison.OrdinalIgnoreCase));

        var badges = await memberBadgeService.GetAllBadgesFor(request.MemberId);
        int badgesAssignedCount = badges.Count;

        bool hasSalesCertificationBadge = badges.Any(b => string.Equals(b.MemberBadgeCodeName, PortalMemberBadges.SALES_CERTIFICATION, StringComparison.OrdinalIgnoreCase));
        bool hasContentModelingCertificationBadge = badges.Any(b => string.Equals(b.MemberBadgeCodeName, PortalMemberBadges.CONTENT_MODELING_CERTIFICATION, StringComparison.OrdinalIgnoreCase));
        bool hasDeveloperOrMarketerCertificationBadge = badges.Any(b =>
            string.Equals(b.MemberBadgeCodeName, PortalMemberBadges.DEVELOPER_CERTIFICATION, StringComparison.OrdinalIgnoreCase)
            || string.Equals(b.MemberBadgeCodeName, PortalMemberBadges.MARKETER_CERTIFICATION, StringComparison.OrdinalIgnoreCase));

        int discussionsCreatedCountYear = await GetDiscussionsCreatedCount(request, yearStart, cancellationToken);

        // Retained for Community Leader fallback display
        int authoredQuestionsCountYear = await GetQuestionsCreatedCount(request, yearStart, cancellationToken);

        return new(
            MemberId: request.MemberId,
            Year: request.Year,
            ProgramStatus: programStatus,
            ActivitiesSubmittedCountYear: activitiesSubmittedCountYear,
            ActivitiesSubmittedCountByQuarter: activitiesSubmittedCountByQuarter,
            SocialPostsCountByQuarter: socialPostsCountByQuarter,
            KenticoFeedbackActivitiesCountYear: kenticoFeedbackActivitiesCountYear,
            DiscussionsCreatedCountYear: discussionsCreatedCountYear,
            BadgesAssignedCount: badgesAssignedCount,
            HasSalesCertificationBadge: hasSalesCertificationBadge,
            HasContentModelingCertificationBadge: hasContentModelingCertificationBadge,
            HasDeveloperOrMarketerCertificationBadge: hasDeveloperOrMarketerCertificationBadge,
            AuthoredQuestionsCountYear: authoredQuestionsCountYear);
    }

    private static int GetQuarterIndex(DateTime date)
        => (date.Month - 1) / 3;

    private async Task<int> GetDiscussionsCreatedCount(CommunityProgramTargetsReportQuery request, DateTime yearStart, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .ForWebsite(request.ChannelName)
                .OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME)
                .WithContentTypeFields())
            .Parameters(q => q
                .Where(w => w
                    .WhereEquals(nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID), request.MemberId)
                    .WhereGreaterOrEquals(nameof(QAndAQuestionPage.QAndAQuestionPageDateCreated), yearStart)
                    .WhereContainsTags(
                        nameof(QAndAQuestionPage.QAndAQuestionPageDiscussionType),
                        [SystemTaxonomies.QAndADiscussionTypeTaxonomy.Question.TagGUID, SystemTaxonomies.QAndADiscussionTypeTaxonomy.Blog.TagGUID]))
                .Columns(nameof(QAndAQuestionPage.SystemFields.WebPageItemID)));

        var pages = await executor.GetMappedWebPageResult<QAndAQuestionPage>(b, new ContentQueryExecutionOptions { ForPreview = false, IncludeSecuredItems = true }, cancellationToken);

        return pages.Count();
    }

    private async Task<int> GetQuestionsCreatedCount(CommunityProgramTargetsReportQuery request, DateTime yearStart, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .ForWebsite(request.ChannelName)
                .OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME)
                .WithContentTypeFields())
            .Parameters(q => q
                .Where(w => w
                    .WhereEquals(nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID), request.MemberId)
                    .WhereGreaterOrEquals(nameof(QAndAQuestionPage.QAndAQuestionPageDateCreated), yearStart)
                    .WhereContainsTags(nameof(QAndAQuestionPage.QAndAQuestionPageDiscussionType), [SystemTaxonomies.QAndADiscussionTypeTaxonomy.Question.TagGUID]))
                .Columns(nameof(QAndAQuestionPage.SystemFields.WebPageItemID)));

        var pages = await executor.GetMappedWebPageResult<QAndAQuestionPage>(b, new ContentQueryExecutionOptions { ForPreview = false, IncludeSecuredItems = true }, cancellationToken);

        return pages.Count();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(CommunityProgramTargetsReportQuery query, CommunityProgramTargetsReportQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllObjects(CommunityProgramActivity_2026Item.CLASS_NAME)
            .AllObjects(MemberInfo.OBJECT_TYPE)
            .AllObjects(MemberBadgeMemberInfo.OBJECT_TYPE)
            .AllObjects(MemberBadgeInfo.OBJECT_TYPE)
            .AllContentItems(QAndAQuestionPage.CONTENT_TYPE_NAME);
}
