using System.Collections.Immutable;
using System.Data;
using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.Reporting;
using Kentico.Community.Portal.Admin.Features.Stats;
using Kentico.Community.Portal.Core;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(StatsPage),
    parentType: typeof(ReportingApplicationPage),
    slug: "stats",
    name: "Stats",
    icon: Icons.Graph,
    templateName: "@kentico-community/portal-web-admin/StatsLayout",
    order: 1)]

namespace Kentico.Community.Portal.Admin.Features.Stats;

public class StatsPage(ISystemClock clock) : Page<StatsPageClientProperties>
{
    public const string IDENTIFIER = "stats";

    private readonly ISystemClock clock = clock;

    public override async Task<StatsPageClientProperties> ConfigureTemplateProperties(StatsPageClientProperties properties)
    {
        properties.Members = await GetMembers();
        properties.Subscribers = await GetSubscribers();
        properties.BlogPosts = await GetBlogPosts();
        properties.Answers = await GetAnswers();
        properties.Questions = await GetQuestions();
        properties.Totals = await GetTotals();

        return properties;
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetMembers()
    {
        string query = $"""
        SELECT 
            FORMAT([MemberCreated], 'yyyy') AS Year,
            FORMAT([MemberCreated], 'MM') AS Month,
            COUNT(*) AS Count
        FROM 
            CMS_Member
        WHERE 
            [MemberCreated] >= DATEADD(MONTH, -12, GETDATE()) AND MemberEnabled = 1
        GROUP BY 
            FORMAT([MemberCreated], 'yyyy'),
            FORMAT([MemberCreated], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetSubscribers()
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
            [EmailSubscriptionConfirmationDate] >= DATEADD(MONTH, -12, GETDATE()) AND esc.EmailSubscriptionConfirmationIsApproved = 1 AND CG.ContactGroupIsRecipientList = 1
        GROUP BY 
            FORMAT([EmailSubscriptionConfirmationDate], 'yyyy'),
            FORMAT([EmailSubscriptionConfirmationDate], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetBlogPosts()
    {
        string query = $"""
        SELECT 
            FORMAT([BlogPostContentPublishedDate], 'yyyy') AS Year,
            FORMAT([BlogPostContentPublishedDate], 'MM') AS Month,
            COUNT(*) AS Count
        FROM KenticoCommunity_BlogPostContent
        WHERE 
            [BlogPostContentPublishedDate] >= DATEADD(MONTH, -12, GETDATE())
        GROUP BY 
            FORMAT([BlogPostContentPublishedDate], 'yyyy'),
            FORMAT([BlogPostContentPublishedDate], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetQuestions()
    {
        string query = $"""
        SELECT 
            FORMAT([QAndAQuestionPageDateCreated], 'yyyy') AS Year,
            FORMAT([QAndAQuestionPageDateCreated], 'MM') AS Month,
            COUNT(*) AS Count
        FROM KenticoCommunity_QAndAQuestionPage
        WHERE 
            [QAndAQuestionPageDateCreated] >= DATEADD(MONTH, -12, GETDATE())
        GROUP BY 
            FORMAT([QAndAQuestionPageDateCreated], 'yyyy'),
            FORMAT([QAndAQuestionPageDateCreated], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query);
    }

    private async Task<ImmutableList<TimeSeriesEntry>> GetAnswers()
    {
        string query = $"""
        SELECT 
            FORMAT([QAndAAnswerDataDateCreated], 'yyyy') AS Year,
            FORMAT([QAndAAnswerDataDateCreated], 'MM') AS Month,
            COUNT(*) AS Count
        FROM KenticoCommunity_QAndAAnswerData
        WHERE 
            [QAndAAnswerDataDateCreated] >= DATEADD(MONTH, -12, GETDATE())
        GROUP BY 
            FORMAT([QAndAAnswerDataDateCreated], 'yyyy'),
            FORMAT([QAndAAnswerDataDateCreated], 'MM')
        ORDER BY 
            Year ASC,
            Month ASC;
        """;

        return await ExecuteTimeSeriesQuery(query);
    }

    private async Task<StatsTotals> GetTotals()
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
                FROM CMS_Member
                WHERE MemberEnabled = 1
            ) AS BlogPostsCount
        """;

        var reader = await ConnectionHelper.ExecuteReaderAsync(query, [], QueryTypeEnum.SQLQuery, CommandBehavior.Default, default);
        var stats = StatsTotals.DEFAULT;

        while (reader.Read())
        {
            int membersCount = reader.GetInt32("MembersCount");
            int subscribersCount = reader.GetInt32("SubscribersCount");
            int questionsCount = reader.GetInt32("QuestionsCount");
            int answersCount = reader.GetInt32("AnswersCount");
            int blogPostsCount = reader.GetInt32("BlogPostsCount");

            stats = new(membersCount, subscribersCount, questionsCount, answersCount, blogPostsCount);
        }

        await reader.CloseAsync();

        return stats;
    }

    public async Task<ImmutableList<TimeSeriesEntry>> ExecuteTimeSeriesQuery(string queryText)
    {
        var reader = await ConnectionHelper.ExecuteReaderAsync(queryText, [], QueryTypeEnum.SQLQuery, CommandBehavior.Default, default);
        var start = clock.Now.AddMonths(-11);
        var entries = Enumerable.Range(0, 12)
            .Select(i => start.AddMonths(i).ToString("MM/yyyy"))
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

public class StatsPageClientProperties : TemplateClientProperties
{
    public StatsTotals Totals { get; set; } = StatsTotals.DEFAULT;
    public ImmutableList<TimeSeriesEntry> Members { get; set; } = [];
    public ImmutableList<TimeSeriesEntry> Subscribers { get; set; } = [];
    public ImmutableList<TimeSeriesEntry> BlogPosts { get; set; } = [];
    public ImmutableList<TimeSeriesEntry> Questions { get; set; } = [];
    public ImmutableList<TimeSeriesEntry> Answers { get; set; } = [];
}

public record StatsTotals(int EnabledMembers, int NewsletterSubscribers, int QAndAQuestions, int QAndAAnswers, int BlogPosts)
{
    public static StatsTotals DEFAULT { get; } = new(0, 0, 0, 0, 0);
}
public record TimeSeriesEntry(string Label, int Value);
