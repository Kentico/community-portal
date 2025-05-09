using System.Collections.Immutable;
using System.Data;
using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.Reporting;
using Kentico.Community.Portal.Admin.Features.Stats;
using Kentico.Community.Portal.Core;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(CommunityStatsPage),
    parentType: typeof(ReportingApplicationPage),
    slug: "stats",
    name: "Stats",
    templateName: "@kentico-community/portal-web-admin/StatsLayout",
    order: 1,
    Icon = Icons.Graph)]

namespace Kentico.Community.Portal.Admin.Features.Stats;

public class CommunityStatsPage(ISystemClock clock) : Page<StatsLayoutClientProperties>
{
    public const string IDENTIFIER = "community-stats";

    private readonly ISystemClock clock = clock;

    public override async Task<StatsLayoutClientProperties> ConfigureTemplateProperties(StatsLayoutClientProperties properties)
    {
        properties.Stats = await GetStats(properties.DefaultSelectedRange);

        return properties;
    }

    [PageCommand(CommandName = "LOADDATA")]
    public async Task<ICommandResponse> PageCommandHandler(int fromMonths)
    {
        var stats = await GetStats(fromMonths);
        return ResponseFrom(stats);
    }

    private async Task<StatsData> GetStats(int fromMonths)
    {
        var startDate = clock.UtcNow.AddMonths(-fromMonths);
        var membersData = await GetMembers(startDate, fromMonths);
        var subscribers = await GetSubscribers(startDate, fromMonths);
        var blogPosts = await GetBlogPosts(startDate, fromMonths);
        var questions = await GetQuestions(startDate, fromMonths);
        var answers = await GetAnswers(startDate, fromMonths);

        var totals = await GetTotals();
        ImmutableList<StatsDatum> data =
        [
            new("New active members", membersData),
            new("New newsletter subscribers", subscribers),
            new("New blog posts", blogPosts),
            new("New Q&A Questions", questions),
            new("New Q&A Answers", answers),
        ];

        return new StatsData(totals, null, data);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetMembers(DateTime startDate, int fromMonths)
    {
        string query = $"""
        SELECT 
            FORMAT([MemberCreated], 'yyyy') AS Year,
            FORMAT([MemberCreated], 'MM') AS Month,
            COUNT(*) AS Count
        FROM 
            CMS_Member
        WHERE 
            [MemberCreated] >= @StartDate AND MemberEnabled = 1
        GROUP BY 
            FORMAT([MemberCreated], 'yyyy'),
            FORMAT([MemberCreated], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query, startDate, fromMonths);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetSubscribers(DateTime startDate, int fromMonths)
    {
        string query = $"""
        SELECT 
            FORMAT([EmailSubscriptionConfirmationDate], 'yyyy') AS Year,
            FORMAT([EmailSubscriptionConfirmationDate], 'MM') AS Month,
            COUNT(*) AS Count
        FROM OM_ContactGroupMember CGM
        INNER JOIN EmailLibrary_EmailSubscriptionConfirmation ESC
            ON CGM.ContactGroupMemberRelatedID = ESC.EmailSubscriptionConfirmationContactID
        INNER JOIN OM_ContactGroup CG
            ON CGM.ContactGroupMemberContactGroupID = CG.ContactGroupID
        WHERE 
            [EmailSubscriptionConfirmationDate] >= @StartDate AND esc.EmailSubscriptionConfirmationIsApproved = 1 AND CG.ContactGroupIsRecipientList = 1
        GROUP BY 
            FORMAT([EmailSubscriptionConfirmationDate], 'yyyy'),
            FORMAT([EmailSubscriptionConfirmationDate], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query, startDate, fromMonths);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetBlogPosts(DateTime startDate, int fromMonths)
    {
        string query = $"""
        SELECT 
            FORMAT([BlogPostPagePublishedDate], 'yyyy') AS Year,
            FORMAT([BlogPostPagePublishedDate], 'MM') AS Month,
            COUNT(*) AS Count
        FROM KenticoCommunity_BlogPostPage
        WHERE 
            [BlogPostPagePublishedDate] >= @StartDate
        GROUP BY 
            FORMAT([BlogPostPagePublishedDate], 'yyyy'),
            FORMAT([BlogPostPagePublishedDate], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query, startDate, fromMonths);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetQuestions(DateTime startDate, int fromMonths)
    {
        string query = $"""
        SELECT 
            FORMAT([QAndAQuestionPageDateCreated], 'yyyy') AS Year,
            FORMAT([QAndAQuestionPageDateCreated], 'MM') AS Month,
            COUNT(*) AS Count
        FROM KenticoCommunity_QAndAQuestionPage
        WHERE 
            [QAndAQuestionPageDateCreated] >= @StartDate
        GROUP BY 
            FORMAT([QAndAQuestionPageDateCreated], 'yyyy'),
            FORMAT([QAndAQuestionPageDateCreated], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query, startDate, fromMonths);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetAnswers(DateTime startDate, int fromMonths)
    {
        string query = $"""
        SELECT 
            FORMAT([QAndAAnswerDataDateCreated], 'yyyy') AS Year,
            FORMAT([QAndAAnswerDataDateCreated], 'MM') AS Month,
            COUNT(*) AS Count
        FROM KenticoCommunity_QAndAAnswerData
        WHERE 
            [QAndAAnswerDataDateCreated] >= @StartDate
        GROUP BY 
            FORMAT([QAndAAnswerDataDateCreated], 'yyyy'),
            FORMAT([QAndAAnswerDataDateCreated], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query, startDate, fromMonths);
    }

    private async Task<ImmutableList<StatsTotal>> GetTotals()
    {
        string query = $"""
        SELECT
            (
                SELECT COUNT(*) 
                FROM KenticoCommunity_QAndAAnswerData
            ) AS AnswersCount,
            (
                SELECT COUNT(*) 
                FROM KenticoCommunity_QAndAQuestionPage
            ) AS QuestionsCount,
            (
                SELECT COUNT(*)
                FROM OM_ContactGroupMember CGM
                INNER JOIN EmailLibrary_EmailSubscriptionConfirmation ESC
                    ON CGM.ContactGroupMemberRelatedID = ESC.EmailSubscriptionConfirmationContactID
                INNER JOIN OM_ContactGroup CG
                    ON CGM.ContactGroupMemberContactGroupID = CG.ContactGroupID
                WHERE ESC.EmailSubscriptionConfirmationIsApproved = 1 AND CG.ContactGroupIsRecipientList = 1
            ) AS SubscribersCount,
            (
                SELECT COUNT(*) 
                FROM CMS_Member
                WHERE MemberEnabled = 1
            ) AS MembersCount,
            (
                SELECT COUNT(*) 
                FROM KenticoCommunity_BlogPostPage
            ) AS BlogPostsCount
        """;

        var reader = await ConnectionHelper.ExecuteReaderAsync(query, [], QueryTypeEnum.SQLQuery, CommandBehavior.Default, default);
        var totals = new List<StatsTotal>();

        while (reader.Read())
        {
            int membersCount = reader.GetInt32("MembersCount");
            int subscribersCount = reader.GetInt32("SubscribersCount");
            int blogPostsCount = reader.GetInt32("BlogPostsCount");
            int questionsCount = reader.GetInt32("QuestionsCount");
            int answersCount = reader.GetInt32("AnswersCount");

            totals = [
                new("Active members", membersCount),
                new("Newsletter subscribers", subscribersCount),
                new("Blog posts", blogPostsCount),
                new("Q&A Questions", questionsCount),
                new("Q&A Answers", answersCount),
            ];
        }

        await reader.CloseAsync();

        return [.. totals];
    }

    public async Task<ImmutableList<TimeSeriesEntry>> ExecuteTimeSeriesQuery(string queryText, DateTime startDate, int fromMonths)
    {
        var qp = new QueryDataParameters
        {
            new DataParameter("@StartDate", startDate)
        };
        var reader = await ConnectionHelper.ExecuteReaderAsync(queryText, qp, QueryTypeEnum.SQLQuery, CommandBehavior.Default, default);
        var start = clock.Now.AddMonths(-11);
        var entries = Enumerable.Range(1, fromMonths)
            .Select(i => startDate.AddMonths(i).ToString("MM/yyyy"))
            .ToDictionary(k => k, v => 0);

        while (reader.Read())
        {
            string year = reader.GetString("Year");
            string month = reader.GetString("Month");
            int count = reader.GetInt32("Count");
            string key = $"{month}/{year}";

            if (entries.ContainsKey(key))
            {
                entries[key] = count;
            }
        }

        await reader.CloseAsync();

        return [.. entries.Select(kv => new TimeSeriesEntry(kv.Key, kv.Value)).ToList()];
    }
}
