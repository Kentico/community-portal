using System.Collections.Immutable;
using System.Data;
using CMS.DataEngine;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Infrastructure;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

[assembly: UIPage(
    uiPageType: typeof(Kentico.Community.Portal.Admin.Features.Reporting.MembershipStatsPage),
    parentType: typeof(Kentico.Community.Portal.Admin.Features.Reporting.ReportingApplicationPage),
    slug: "membership-stats",
    name: "Membership",
    templateName: "@kentico-community/portal-web-admin/MembershipStatsLayout",
    order: 2,
    Icon = Icons.Graph)]

namespace Kentico.Community.Portal.Admin.Features.Reporting;

public class MembershipStatsPage(
    IWebsiteChannelDomainProvider domainProvider,
    IHttpContextAccessor contextAccessor,
    AdminLinkBuilder adminLinkBuilder) : Page<MembershipStatsLayoutClientProperties>
{
    private const string MemberDisplayNameExpression = "COALESCE(NULLIF(LTRIM(RTRIM(COALESCE(M.[MemberFirstName], '') + ' ' + COALESCE(M.[MemberLastName], ''))), ''), M.[MemberName])";
    private readonly IWebsiteChannelDomainProvider domainProvider = domainProvider;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly AdminLinkBuilder adminLinkBuilder = adminLinkBuilder;

    private static readonly ImmutableArray<LeaderboardDefinition> leaderboardDefinitions =
    [
        new("link-content", "Members with the most community contributions"),
        new("integration-content", "Members with the most integrations"),
        new("questions", "Members with the most Questions associated"),
        new("answers", "Members with the most Answers associated"),
    ];

    public override async Task<MembershipStatsLayoutClientProperties> ConfigureTemplateProperties(MembershipStatsLayoutClientProperties properties)
    {
        properties.Stats = await GetDashboard(CancellationToken.None);
        return properties;
    }

    private async Task<MembershipStatsDashboard> GetDashboard(CancellationToken cancellationToken)
    {
        var moderationStatuses = await GetModerationStatuses(cancellationToken);
        var domainCounts = await GetEmailDomainCounts(cancellationToken);
        var leaderboards = await GetContributionLeaderboards(cancellationToken);
        var programRoleCounts = await GetProgramRoleCounts(cancellationToken);

        var overview = new MembershipStatsOverview(
            domainCounts.EnabledEmailDomains.Sum(item => item.Count),
            moderationStatuses.Sum(item => item.Value),
            domainCounts.EnabledEmailDomains.Count,
            domainCounts.ModeratedEmailDomains.Count,
            programRoleCounts.MvpCount,
            programRoleCounts.CommunityLeaderCount);

        return new(
            overview,
            moderationStatuses,
            domainCounts.EnabledEmailDomains,
            domainCounts.ModeratedEmailDomains,
            leaderboards);
    }

    private static async Task<ImmutableList<MemberModerationStatusCount>> GetModerationStatuses(CancellationToken cancellationToken)
    {
        string defaultStatus = ModerationStatuses.None.ToString();
        string query = $"""
        SELECT
            COALESCE(NULLIF([{CommunityMemberInfoExtensions.FIELD_MODERATION_STATUS}], ''), @DefaultStatus) AS [ModerationStatus],
            COUNT(*) AS [Count]
        FROM [CMS_Member]
        WHERE [MemberEnabled] = 0
        GROUP BY COALESCE(NULLIF([{CommunityMemberInfoExtensions.FIELD_MODERATION_STATUS}], ''), @DefaultStatus)
        """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@DefaultStatus", defaultStatus),
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        var countsByStatus = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            countsByStatus[reader.GetString("ModerationStatus")] = reader.GetInt32("Count");
        }

        return
        [
            .. Enum.GetValues<ModerationStatuses>()
                .Where(status => status != ModerationStatuses.None)
                .Select(status => new MemberModerationStatusCount(
                    status.ToString(),
                    GetModerationLabel(status),
                    countsByStatus.GetValueOrDefault(status.ToString())))
        ];
    }

    private static async Task<ProgramRoleCountResult> GetProgramRoleCounts(CancellationToken cancellationToken)
    {
        const string query = """
        SELECT
            R.[MemberRoleGUID] AS [RoleGUID],
            COUNT(DISTINCT RM.[MemberRoleMemberMemberID]) AS [Count]
        FROM [CMS_MemberRoleMember] RM
        INNER JOIN [CMS_MemberRole] R
            ON RM.[MemberRoleMemberMemberRoleID] = R.[MemberRoleID]
        WHERE R.[MemberRoleGUID] IN (@MvpRoleGUID, @CommunityLeaderRoleGUID)
        GROUP BY R.[MemberRoleGUID]
        """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@MvpRoleGUID", SystemMemberRoles.KenticoMVP.MemberRoleGUID),
            new DataParameter("@CommunityLeaderRoleGUID", SystemMemberRoles.KenticoCommunityLeader.MemberRoleGUID),
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        int mvpCount = 0;
        int communityLeaderCount = 0;

        while (reader.Read())
        {
            var roleGuid = reader.GetGuid("RoleGUID");
            int count = reader.GetInt32("Count");

            if (roleGuid == SystemMemberRoles.KenticoMVP.MemberRoleGUID)
            {
                mvpCount = count;
            }
            else if (roleGuid == SystemMemberRoles.KenticoCommunityLeader.MemberRoleGUID)
            {
                communityLeaderCount = count;
            }
        }

        return new(mvpCount, communityLeaderCount);
    }

    private static async Task<DomainCountResult> GetEmailDomainCounts(CancellationToken cancellationToken)
    {
        string defaultStatus = ModerationStatuses.None.ToString();
        string query = $"""
        WITH [MemberDomains] AS (
            SELECT
                LOWER(LTRIM(RTRIM(SUBSTRING([MemberEmail], CHARINDEX('@', [MemberEmail]) + 1, LEN([MemberEmail]))))) AS [Domain],
                CASE WHEN [MemberEnabled] = 1 THEN 1 ELSE 0 END AS [EnabledCount],
                CASE
                    WHEN [MemberEnabled] = 0
                        AND COALESCE(NULLIF([{CommunityMemberInfoExtensions.FIELD_MODERATION_STATUS}], ''), @DefaultStatus) <> @DefaultStatus
                        THEN 1
                    ELSE 0
                END AS [ModeratedCount]
            FROM [CMS_Member]
            WHERE [MemberEmail] IS NOT NULL
                AND LTRIM(RTRIM([MemberEmail])) <> ''
                AND CHARINDEX('@', [MemberEmail]) > 1
                AND LEN([MemberEmail]) > CHARINDEX('@', [MemberEmail])
        )
        SELECT
            [Domain],
            SUM([EnabledCount]) AS [EnabledCount],
            SUM([ModeratedCount]) AS [ModeratedCount]
        FROM [MemberDomains]
        GROUP BY [Domain]
        HAVING SUM([EnabledCount]) > 0 OR SUM([ModeratedCount]) > 0
        ORDER BY [Domain] ASC
        """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@DefaultStatus", defaultStatus),
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        var enabled = new List<MemberEmailDomainCount>();
        var moderated = new List<MemberEmailDomainCount>();

        while (reader.Read())
        {
            string domain = reader.GetString("Domain");
            int enabledCount = reader.GetInt32("EnabledCount");
            int moderatedCount = reader.GetInt32("ModeratedCount");

            if (enabledCount > 0)
            {
                enabled.Add(new(domain, enabledCount));
            }

            if (moderatedCount > 0)
            {
                moderated.Add(new(domain, moderatedCount));
            }
        }

        return new(
            [.. enabled.OrderByDescending(item => item.Count).ThenBy(item => item.Domain)],
            [.. moderated.OrderByDescending(item => item.Count).ThenBy(item => item.Domain)]);
    }

    private async Task<ImmutableList<MemberContributionLeaderboard>> GetContributionLeaderboards(CancellationToken cancellationToken)
    {
        string query = $"""
        WITH [MemberActivity] AS (
            SELECT
                'link-content' AS [Key],
                [LinkContentMemberID] AS [MemberID],
                COUNT(*) AS [ItemCount]
            FROM [KenticoCommunity_LinkContent]
            WHERE [LinkContentMemberID] > 0
            GROUP BY [LinkContentMemberID]

            UNION ALL

            SELECT
                'integration-content' AS [Key],
                [IntegrationContentAuthorMemberID] AS [MemberID],
                COUNT(*) AS [ItemCount]
            FROM [KenticoCommunity_IntegrationContent]
            WHERE [IntegrationContentAuthorMemberID] > 0
            GROUP BY [IntegrationContentAuthorMemberID]

            UNION ALL

            SELECT
                'questions' AS [Key],
                [QAndAQuestionPageAuthorMemberID] AS [MemberID],
                COUNT(*) AS [ItemCount]
            FROM [KenticoCommunity_QAndAQuestionPage]
            WHERE [QAndAQuestionPageAuthorMemberID] > 0
            GROUP BY [QAndAQuestionPageAuthorMemberID]

            UNION ALL

            SELECT
                'answers' AS [Key],
                [QAndAAnswerDataAuthorMemberID] AS [MemberID],
                COUNT(*) AS [ItemCount]
            FROM [KenticoCommunity_QAndAAnswerData]
            WHERE [QAndAAnswerDataAuthorMemberID] > 0
            GROUP BY [QAndAAnswerDataAuthorMemberID]
        ),
        [RankedActivity] AS (
            SELECT
                A.[Key],
                A.[MemberID],
                A.[ItemCount],
                {MemberDisplayNameExpression} AS [DisplayName],
                M.[MemberEmail],
                ROW_NUMBER() OVER (
                    PARTITION BY A.[Key]
                    ORDER BY A.[ItemCount] DESC, {MemberDisplayNameExpression} ASC, A.[MemberID] ASC
                ) AS [RowNumber]
            FROM [MemberActivity] A
            INNER JOIN [CMS_Member] M
                ON A.[MemberID] = M.[MemberID]
            WHERE M.[MemberEnabled] = 1
        )
        SELECT
            [Key],
            [MemberID],
            [DisplayName],
            [MemberEmail],
            [ItemCount]
        FROM [RankedActivity]
        WHERE [RowNumber] <= @TopCount
        ORDER BY [Key] ASC, [RowNumber] ASC
        """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@TopCount", 20),
        };

        int websiteChannelId = await GetWebsiteChannelId(cancellationToken);
        string memberDomain = websiteChannelId > 0
            ? await domainProvider.GetDomain(websiteChannelId, default) ?? string.Empty
            : string.Empty;
        string requestScheme = contextAccessor.HttpContext?.Request.Scheme ?? Uri.UriSchemeHttps;

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        var itemsByKey = new Dictionary<string, List<MemberContributionLeader>>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            string key = reader.GetString("Key");
            if (!itemsByKey.TryGetValue(key, out var members))
            {
                members = [];
                itemsByKey[key] = members;
            }

            members.Add(new(
                reader.GetInt32("MemberID"),
                reader.GetString("DisplayName"),
                reader.GetString("MemberEmail"),
                reader.GetInt32("ItemCount"),
                adminLinkBuilder.GetPath<MemberEdit>(new()
                {
                    { typeof(MemberEditSection), reader.GetInt32("MemberID").ToString() },
                }),
                BuildMemberViewUrl(reader.GetInt32("MemberID"), memberDomain, requestScheme)));
        }

        return
        [
            .. leaderboardDefinitions.Select(definition => new MemberContributionLeaderboard(
                definition.Key,
                definition.Label,
                [.. itemsByKey.TryGetValue(definition.Key, out var members)
                    ? members
                    : Enumerable.Empty<MemberContributionLeader>()]))
        ];
    }

    private static async Task<int> GetWebsiteChannelId(CancellationToken cancellationToken)
    {
        const string query = """
        SELECT TOP (1) WC.[WebsiteChannelID]
        FROM [CMS_WebsiteChannel] WC
        INNER JOIN [CMS_Channel] C
            ON WC.[WebsiteChannelChannelID] = C.[ChannelID]
        WHERE C.[ChannelName] = @ChannelCodeName
        """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@ChannelCodeName", PortalWebSiteChannel.CODE_NAME),
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        if (!reader.Read())
        {
            return 0;
        }

        return reader.GetInt32("WebsiteChannelID");
    }

    private static string GetModerationLabel(ModerationStatuses status) => status switch
    {
        ModerationStatuses.Spam => "Spam",
        ModerationStatuses.Flagged => "Flagged",
        ModerationStatuses.Archived => "Archived",
        ModerationStatuses.None or _ => status.ToString(),
    };

    private static string BuildMemberViewUrl(int memberId, string domain, string requestScheme)
    {
        string relativeUrl = $"/member/{memberId}";

        if (string.IsNullOrWhiteSpace(domain))
        {
            return relativeUrl;
        }

        return UriHelper.BuildAbsolute(
            scheme: requestScheme,
            host: new HostString(domain),
            pathBase: string.Empty,
            path: relativeUrl);
    }

    private sealed record DomainCountResult(
        ImmutableList<MemberEmailDomainCount> EnabledEmailDomains,
        ImmutableList<MemberEmailDomainCount> ModeratedEmailDomains);

    private sealed record ProgramRoleCountResult(int MvpCount, int CommunityLeaderCount);

    private sealed record LeaderboardDefinition(string Key, string Label);
}
