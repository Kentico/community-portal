using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Xperience.Lucene.Indexing;
using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndASearchModel
{
    public const string IndexName = "QANDA_SEARCH";

    public int ID { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime PublishedDate { get; set; }
    public DateTime LatestResponseDate { get; set; }
    public int AuthorMemberID { get; set; }
    public string AuthorUsername { get; set; } = "";
    public string AuthorFullName { get; set; } = "";
    public bool HasAcceptedResponse { get; set; }
    public string DiscussionType { get; set; } = "";
    public IEnumerable<string> Topics { get; set; } = [];
    public int ResponseCount { get; set; } = 0;

    public Document ToDocument()
    {
        var indexDocument = new Document()
        {
            new Int32Field(nameof(ID), ID, Field.Store.YES),
            new TextField(nameof(Title), Title, Field.Store.YES),
            new TextField(nameof(Content), Content, Field.Store.NO),
            new Int64Field(nameof(PublishedDate), DateTools.TicksToUnixTimeMilliseconds(PublishedDate.Ticks), Field.Store.YES),
            new Int64Field(nameof(LatestResponseDate), DateTools.TicksToUnixTimeMilliseconds(LatestResponseDate.Ticks), Field.Store.YES),
            new Int32Field(nameof(AuthorMemberID), AuthorMemberID, Field.Store.YES),
            new TextField(nameof(AuthorUsername), AuthorUsername, Field.Store.YES),
            new TextField(nameof(AuthorFullName), AuthorFullName, Field.Store.YES),
            new Int32Field(nameof(HasAcceptedResponse), HasAcceptedResponse ? 1 : 0, Field.Store.YES),
            new Int32Field(nameof(ResponseCount), ResponseCount, Field.Store.YES),
            new FacetField(nameof(DiscussionType), (string.IsNullOrWhiteSpace(DiscussionType) ? "None" : DiscussionType).ToLowerInvariant()),
        };

        if (Topics.Any())
        {
            indexDocument.Add(new FacetField(nameof(Topics), Topics.Select(t => t.ToLowerInvariant()).ToArray()));
        }

        return indexDocument;
    }

    public static DateTime DefaultTime { get; } = new DateTime(1900, 1, 1);

    public static QAndASearchModel FromDocument(Document doc)
    {
        var model = new QAndASearchModel
        {
            ID = int.TryParse(doc.Get(nameof(ID)), out int id)
                ? id
                : 0,
            Title = doc.Get(nameof(Title)),
            Url = doc.Get(nameof(Url)) ?? "",
            Content = doc.Get(nameof(Content)),
            AuthorUsername = doc.Get(nameof(AuthorUsername)),
            AuthorFullName = doc.Get(nameof(AuthorFullName)),
            AuthorMemberID = int.TryParse(doc.Get(nameof(AuthorMemberID)), out int authorMemberID)
                ? authorMemberID
                : 0,
            HasAcceptedResponse = doc.Get(nameof(HasAcceptedResponse)) switch
            {
                "1" => true,
                "0" or _ => false
            },
            ResponseCount = int.TryParse(doc.Get(nameof(ResponseCount)), out int answerCount)
                ? answerCount
                : 0,
            PublishedDate = new DateTime(
                DateTools.UnixTimeMillisecondsToTicks(
                    long.TryParse(doc.Get(nameof(PublishedDate)), out long pubVal) ? pubVal : DateTools.TicksToUnixTimeMilliseconds(DefaultTime.Ticks)
                )),
            LatestResponseDate = new DateTime(
                DateTools.UnixTimeMillisecondsToTicks(
                    long.TryParse(doc.Get(nameof(LatestResponseDate)), out long responseVal) ? responseVal : DateTools.TicksToUnixTimeMilliseconds(DefaultTime.Ticks)
                )),
            DiscussionType = doc.Get(nameof(DiscussionType)) ?? "",
            Topics = doc.GetValues(nameof(Topics)) ?? []
        };

        return model;
    }
}

public class QAndASearchIndexingStrategy(
    IContentQueryExecutor executor,
    WebScraperHtmlSanitizer htmlSanitizer,
    IInfoProvider<MemberInfo> memberProvider,
    ITaxonomyRetriever taxonomyRetriever,
    IInfoProvider<QAndAAnswerDataInfo> answerProvider) : DefaultLuceneIndexingStrategy
{
    public const string IDENTIFIER = "QANDA_SEARCH";

    private readonly IContentQueryExecutor executor = executor;
    private readonly WebScraperHtmlSanitizer htmlSanitizer = htmlSanitizer;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IInfoProvider<QAndAAnswerDataInfo> answerProvider = answerProvider;

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        if (item is not IndexEventWebPageItemModel webpageItem)
        {
            return null;
        }

        var qandaModel = new QAndASearchModel();

        var b = new ContentItemQueryBuilder()
            .ForWebPage(QAndAQuestionPage.CONTENT_TYPE_NAME, item.ItemGuid, p => p.WithLinkedItems(1));

        var page = (await executor.GetMappedWebPageResult<QAndAQuestionPage>(b)).FirstOrDefault();
        if (page is null)
        {
            return null;
        }

        var author = await GetAuthor(executor, memberProvider, page);
        qandaModel.ID = page.SystemFields.WebPageItemID;
        qandaModel.Title = page.QAndAQuestionPageTitle;
        qandaModel.AuthorUsername = author.Username;
        qandaModel.AuthorFullName = author.FullName;
        qandaModel.AuthorMemberID = author.MemberID;
        qandaModel.Content = htmlSanitizer.SanitizeHtmlDocument(page.QAndAQuestionPageContent);
        qandaModel.PublishedDate = page.QAndAQuestionPageDateCreated != default
            ? page.QAndAQuestionPageDateCreated
            : DateTime.MinValue;
        qandaModel.HasAcceptedResponse = page.QAndAQuestionPageAcceptedAnswerDataGUID != default;
        qandaModel.DiscussionType = await GetDiscussionType(page, item);
        qandaModel.Topics = await GetTopics(page, item);

        var answers = (await answerProvider
            .Get()
            .WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), page.SystemFields.WebPageItemID)
            .Columns(nameof(QAndAAnswerDataInfo.QAndAAnswerDataID), nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateCreated))
            .GetEnumerableTypedResultAsync())
            .ToList();

        qandaModel.ResponseCount = answers.Count;
        qandaModel.LatestResponseDate = answers.Count > 0
            ? answers.Max(a => a.QAndAAnswerDataDateCreated)
            : QAndASearchModel.DefaultTime;

        return qandaModel.ToDocument();
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(nameof(QAndASearchModel.Topics), true);
        facetConfig.SetMultiValued(nameof(QAndASearchModel.DiscussionType), false);

        return facetConfig;
    }

    private record QAndAAuthor(int MemberID, string Username, string FullName);

    private static async Task<QAndAAuthor> GetAuthor(
        IContentQueryExecutor executor,
        IInfoProvider<MemberInfo> memberProvider,
        QAndAQuestionPage page)
    {
        var member = (await memberProvider.Get()
            .WhereEquals(nameof(MemberInfo.MemberID), page.QAndAQuestionPageAuthorMemberID)
            .GetEnumerableTypedResultAsync()).FirstOrDefault();

        if (member is not null)
        {
            var cm = CommunityMember.FromMemberInfo(member);
            return new(cm.Id, cm.UserName!, cm.FullName);
        }

        var b = new ContentItemQueryBuilder()
            .ForContentType(AuthorContent.CONTENT_TYPE_NAME, queryParameters =>
            {
                _ = queryParameters.Where(w => w.WhereEquals(nameof(AuthorContent.AuthorContentCodeName), AuthorContent.KENTICO_AUTHOR_CODE_NAME));
            });

        var authors = await executor.GetMappedResult<AuthorContent>(b);
        var author = authors.First();

        return new(0, author.AuthorContentCodeName, author.FullName);
    }

    private async Task<string> GetDiscussionType(QAndAQuestionPage page, IIndexEventItemModel item)
    {
        var tag = (await taxonomyRetriever.RetrieveTags(page.QAndAQuestionPageDiscussionType.Select(t => t.Identifier), item.LanguageName)).FirstOrDefault();

        return tag?.Title ?? "";
    }

    private async Task<IEnumerable<string>> GetTopics(QAndAQuestionPage page, IIndexEventItemModel item)
    {
        var tags = await taxonomyRetriever.RetrieveTags(page.QAndAQuestionPageDXTopics.Select(t => t.Identifier), item.LanguageName);

        return tags.Select(t => t.Title);
    }
}

public class QAndASearchRequest
{
    public const int PAGE_SIZE = 20;

    public QAndASearchRequest(HttpRequest request)
    {
        var query = request.Query;

        Facet = query.TryGetValue("facet", out var facetValues)
            ? facetValues.ToString()
            : "";
        SearchText = query.TryGetValue("query", out var queryValues)
            ? queryValues.ToString()
            : "";
        SortBy = query.TryGetValue("sortBy", out var sortByValues)
            ? sortByValues.ToString()
            : "publishdate";
        PageNumber = query.TryGetValue("page", out var pageValues)
            ? int.TryParse(pageValues, out int p)
                ? p
                : 1
            : 1;

        AuthorMemberID = query.TryGetValue("author", out var authorValues)
            ? int.TryParse(authorValues, out int a)
                ? a
                : 0
            : 0;

        OnlyAcceptedResponses = query.TryGetValue(nameof(OnlyAcceptedResponses), out var acceptedResponsesValues)
            && acceptedResponsesValues.Count > 0 && bool.TryParse(acceptedResponsesValues[0], out bool ar)
            && ar;
    }

    public QAndASearchRequest(string sortBy, int pageSize)
    {
        SortBy = sortBy;
        PageSize = pageSize;
    }

    public string SearchText { get; } = "";
    public string SortBy { get; } = "";
    public int PageNumber { get; } = 1;
    public int PageSize { get; } = PAGE_SIZE;
    public int AuthorMemberID { get; set; }
    public bool OnlyAcceptedResponses { get; set; }
    public string Facet { get; set; } = "";

    public bool AreFiltersDefault => string.IsNullOrWhiteSpace(SearchText) && AuthorMemberID < 1 && !OnlyAcceptedResponses;
}
