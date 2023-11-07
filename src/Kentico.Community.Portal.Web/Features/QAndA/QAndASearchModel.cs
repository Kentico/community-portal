using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Documents;

namespace Kentico.Community.Portal.Web.Features.QAndA;

[IncludedPath("/", ContentTypes = new string[] {
    QAndAQuestionPage.CONTENT_TYPE_NAME
})]
public class QAndASearchModel : LuceneSearchModel
{
    public const string IndexName = "QAndASearchModel";

    [Int32Field(true)]
    public int ID { get; set; }
    [TextField(true)]
    public string Title { get; set; } = "";
    [TextField(false)]
    public string Content { get; set; } = "";
    [Int64Field(true)]
    public long DateMilliseconds { get; set; }
    [Int32Field(true)]
    public int AuthorMemberID { get; set; }
    [TextField(true)]
    public string AuthorUsername { get; set; } = "";
    [TextField(true)]
    public string AuthorFullName { get; set; } = "";
    [Int32Field(true)]
    public int IsAnswered { get; set; } = 0;
    [Int32Field(true)]
    public int AnswerCount { get; set; } = 0;
}

public class QAndASearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public override async Task<LuceneSearchModel> OnIndexingNode(IndexedItemModel lucenePageItem, LuceneSearchModel model)
    {
        var webPageMapper = Service.Resolve<IWebPageQueryResultMapper>();
        var contentItemMapper = Service.Resolve<IContentQueryResultMapper>();
        var executor = Service.Resolve<IContentQueryExecutor>();
        var htmlSanitizer = Service.Resolve<WebScraperHtmlSanitizer>();
        var memberProvider = Service.Resolve<IInfoProvider<MemberInfo>>();
        var answerProvider = Service.Resolve<IQAndAAnswerDataInfoProvider>();

        var blogModel = new QAndASearchModel
        {
            Url = model.Url,
            ObjectID = model.ObjectID
        };

        var b = new ContentItemQueryBuilder()
            .ForContentType(QAndAQuestionPage.CONTENT_TYPE_NAME, queryParameters =>
            {
                _ = queryParameters
                    .WithLinkedItems(1)
                    .ForWebsite(lucenePageItem.ChannelName)
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemGUID), lucenePageItem.WebPageItemGuid));
            });

        var page = (await executor.GetWebPageResult(b, webPageMapper.Map<QAndAQuestionPage>)).FirstOrDefault();
        if (page is not null)
        {
            var author = await GetAuthor(executor, contentItemMapper, memberProvider, page);
            blogModel.ID = page.SystemFields.WebPageItemID;
            blogModel.Title = page.QAndAQuestionPageTitle;
            blogModel.AuthorUsername = author.Username;
            blogModel.AuthorFullName = author.FullName;
            blogModel.AuthorMemberID = author.MemberID;
            blogModel.Content = htmlSanitizer.SanitizeHtmlDocument(page.QAndAQuestionPageContent);

            var date = page.QAndAQuestionPageDateCreated != default
                ? page.QAndAQuestionPageDateCreated
                : DateTime.MinValue;

            blogModel.DateMilliseconds = DateTools.TicksToUnixTimeMilliseconds(date.Ticks);

            blogModel.IsAnswered = page.QAndAQuestionPageAcceptedAnswerDataGUID == default
                ? 0
                : 1;

            var answers = await answerProvider
                .Get()
                .WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), page.SystemFields.WebPageItemID)
                .Columns(nameof(QAndAAnswerDataInfo.QAndAAnswerDataID))
                .GetEnumerableTypedResultAsync();

            blogModel.AnswerCount = answers.Count();
        }

        return blogModel;
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
            return new(cm.Id, cm.UserName, cm.FullName);
        }

        var b = new ContentItemQueryBuilder()
            .ForContentType(AuthorContent.CONTENT_TYPE_NAME, queryParameters =>
            {
                queryParameters.Where(w => w.WhereEquals(nameof(AuthorContent.AuthorContentCodeName), AuthorContent.KENTICO_AUTHOR_CODE_NAME));
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
            : "date";
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

public class QAndASearchResult
{
    public int ID { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public int AuthorMemberID { get; set; }
    public string AuthorUsername { get; set; } = "";
    public string AuthorFullName { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public bool IsAnswered { get; set; }
    public int AnswerCount { get; set; }

    public static QAndASearchResult MapFromDocument(Document doc)
    {
        int id = int.TryParse(doc.Get(nameof(QAndASearchModel.ID)), out int modelID)
            ? modelID
            : 0;
        string url = doc.Get(nameof(QAndASearchModel.Url)) ?? "";
        string title = doc.Get(nameof(QAndASearchModel.Title));
        int authorMemberID = int.TryParse(doc.Get(nameof(QAndASearchModel.AuthorMemberID)), out int memberID)
            ? memberID
            : 0;
        string username = doc.Get(nameof(QAndASearchModel.AuthorUsername));
        string fullname = doc.Get(nameof(QAndASearchModel.AuthorFullName));
        long ticks = DateTools.UnixTimeMillisecondsToTicks(
            long.TryParse(doc.Get(nameof(QAndASearchModel.DateMilliseconds)), out long milliseconds)
                ? milliseconds
                : 0);
        bool isAnswered = doc.Get(nameof(QAndASearchModel.IsAnswered)) switch
        {
            "1" => true,
            "0" or _ => false
        };
        int answerCount = int.TryParse(doc.Get(nameof(QAndASearchModel.AnswerCount)), out int count)
                ? count
                : 0;

        return new()
        {
            ID = id,
            Url = url,
            Title = title,
            AuthorMemberID = authorMemberID,
            AuthorUsername = username,
            AuthorFullName = fullname,
            DateCreated = new DateTime(ticks),
            IsAnswered = isAnswered,
            AnswerCount = answerCount
        };
    }
}
