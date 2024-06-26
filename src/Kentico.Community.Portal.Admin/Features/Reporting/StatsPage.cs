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
        properties.Answers = await GetAnswers();
        properties.Questions = await GetQuestions();

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
        WHERE 
            [EmailSubscriptionConfirmationDate] >= DATEADD(MONTH, -12, GETDATE()) AND esc.EmailSubscriptionConfirmationIsApproved = 1
        GROUP BY 
            FORMAT([EmailSubscriptionConfirmationDate], 'yyyy'),
            FORMAT([EmailSubscriptionConfirmationDate], 'MM')
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
    public ImmutableList<TimeSeriesEntry> Members { get; set; } = [];
    public ImmutableList<TimeSeriesEntry> Subscribers { get; set; } = [];
    public ImmutableList<TimeSeriesEntry> Questions { get; set; } = [];
    public ImmutableList<TimeSeriesEntry> Answers { get; set; } = [];
}

public record TimeSeriesEntry(string Label, int Value);
