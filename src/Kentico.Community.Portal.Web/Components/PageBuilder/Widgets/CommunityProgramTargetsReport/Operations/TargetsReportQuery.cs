using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport.Operations;

public record TargetsReportQuery(
    int MemberId,
    string ChannelName,
    int Year)
    : IQuery<TargetsReportQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => $"{ChannelName}|{MemberId}|{Year}";
}

public record TargetsReportQueryResponse(
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

public class TargetsReportQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<MemberInfo> memberProvider,
    MemberBadgeService memberBadgeService,
    IContentQueryExecutor executor) : DataItemQueryHandler<TargetsReportQuery, TargetsReportQueryResponse>(tools)
{
    private const string SocialPostActivityType = "Social media promotion";

    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly MemberBadgeService memberBadgeService = memberBadgeService;
    private readonly IContentQueryExecutor executor = executor;

    public override async Task<TargetsReportQueryResponse> Handle(TargetsReportQuery request, CancellationToken cancellationToken = default)
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

    private async Task<int> GetDiscussionsCreatedCount(TargetsReportQuery request, DateTime yearStart, CancellationToken cancellationToken)
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

    private async Task<int> GetQuestionsCreatedCount(TargetsReportQuery request, DateTime yearStart, CancellationToken cancellationToken)
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

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(TargetsReportQuery query, TargetsReportQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllObjects(CommunityProgramActivity_2026Item.CLASS_NAME)
            .AllObjects(MemberInfo.OBJECT_TYPE)
            .AllObjects(MemberBadgeMemberInfo.OBJECT_TYPE)
            .AllObjects(MemberBadgeInfo.OBJECT_TYPE)
            .AllContentItems(QAndAQuestionPage.CONTENT_TYPE_NAME);
}
