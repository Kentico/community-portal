using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Xperience.Lucene.Indexing;
using Lucene.Net.Documents;

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
        };

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
                ))
        };

        return model;
    }
}

public class QAndASearchIndexingStrategy(
    IWebPageQueryResultMapper webPageMapper,
    IContentQueryResultMapper contentItemMapper,
    IContentQueryExecutor executor,
    WebScraperHtmlSanitizer htmlSanitizer,
    IInfoProvider<MemberInfo> memberProvider,
    IQAndAAnswerDataInfoProvider answerProvider
    ) : DefaultLuceneIndexingStrategy
{
    public const string IDENTIFIER = "QANDA_SEARCH";

    // TODO - temporary until more advanced web page querying is available : https://roadmap.kentico.com/c/193-new-api-cross-content-type-querying
    public const string WEBSITE_CHANNEL_NAME = "devnet";

    private readonly IWebPageQueryResultMapper webPageMapper = webPageMapper;
    private readonly IContentQueryResultMapper contentItemMapper = contentItemMapper;
    private readonly IContentQueryExecutor executor = executor;
    private readonly WebScraperHtmlSanitizer htmlSanitizer = htmlSanitizer;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IQAndAAnswerDataInfoProvider answerProvider = answerProvider;

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        if (item is not IndexEventWebPageItemModel webpageItem)
        {
            return null;
        }

        var qandaModel = new QAndASearchModel();

        var b = new ContentItemQueryBuilder()
            .ForWebPage(webpageItem.WebsiteChannelName, QAndAQuestionPage.CONTENT_TYPE_NAME, item.ItemGuid, queryParameters => queryParameters.WithLinkedItems(1));

        var page = (await executor.GetWebPageResult(b, webPageMapper.Map<QAndAQuestionPage>)).FirstOrDefault();
        if (page is null)
        {
            return null;
        }

        var author = await GetAuthor(executor, contentItemMapper, memberProvider, page);
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

    private record QAndAAuthor(int MemberID, string Username, string FullName);

    private static async Task<QAndAAuthor> GetAuthor(
        IContentQueryExecutor executor,
        IContentQueryResultMapper mapper,
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

        var authors = await executor.GetResult(b, mapper.Map<AuthorContent>);
        var author = authors.First();

        return new(0, author.AuthorContentCodeName, author.FullName);
    }
}

public class QAndASearchRequest
{
    public const int PAGE_SIZE = 20;

    public QAndASearchRequest(HttpRequest request)
    {
        var query = request.Query;

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
    }

    public QAndASearchRequest(string sortBy, int pageSize)
    {
        SortBy = sortBy;
        PageSize = pageSize;
    }

    public void Deconstruct(out string searchText, out string sortBy, out int pageNumber, out int pageSize, out int authorMemberID)
    {
        searchText = SearchText;
        sortBy = SortBy;
        pageNumber = PageNumber;
        pageSize = PageSize;
        authorMemberID = AuthorMemberID;
    }

    public string SearchText { get; } = "";
    public string SortBy { get; } = "";
    public int PageNumber { get; } = 1;
    public int PageSize { get; } = PAGE_SIZE;
    public int AuthorMemberID { get; set; }

    public bool AreFiltersDefault => string.IsNullOrWhiteSpace(SearchText) && AuthorMemberID < 1;
}
