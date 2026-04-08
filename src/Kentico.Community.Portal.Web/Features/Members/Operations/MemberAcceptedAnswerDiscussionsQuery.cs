using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Members;

public record AcceptedAnswerDiscussion(string Url, string Title, DateTime Date);

public record MemberAcceptedAnswerDiscussionsQuery(int MemberID) : IQuery<MemberAcceptedAnswerDiscussionsQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => MemberID.ToString();
}

public record MemberAcceptedAnswerDiscussionsQueryResponse(IReadOnlyList<AcceptedAnswerDiscussion> Items);

public class MemberAcceptedAnswerDiscussionsQueryHandler(
    WebPageQueryTools tools,
    IInfoProvider<QAndAAnswerDataInfo> answerProvider,
    IWebsiteChannelContext channelContext)
    : WebPageQueryHandler<MemberAcceptedAnswerDiscussionsQuery, MemberAcceptedAnswerDiscussionsQueryResponse>(tools)
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> answerProvider = answerProvider;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    public override async Task<MemberAcceptedAnswerDiscussionsQueryResponse> Handle(MemberAcceptedAnswerDiscussionsQuery request, CancellationToken cancellationToken = default)
    {
        // Get question page item IDs where the member's answer was the accepted answer
        var acceptedAnswers = await answerProvider.Get()
            .Source(s => s.Join(
                "KenticoCommunity_QAndAQuestionPage",
                nameof(QAndAAnswerDataInfo.QAndAAnswerDataGUID),
                nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID)))
            .WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID), request.MemberID)
            .Columns(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var questionPageItemIDs = acceptedAnswers
            .Select(a => a.QAndAAnswerDataQuestionWebPageItemID)
            .Distinct()
            .ToList();

        if (questionPageItemIDs.Count == 0)
        {
            return new([]);
        }

        var b = new ContentItemQueryBuilder()
            .ForContentType(QAndAQuestionPage.CONTENT_TYPE_NAME, q => q
                .ForWebsite(channelContext.WebsiteChannelName)
                .Where(w => w.WhereIn(nameof(WebPageFields.WebPageItemID), questionPageItemIDs))
                .WithLinkedItems(0));

        var pages = await Executor.GetMappedWebPageResult<QAndAQuestionPage>(b, DefaultQueryOptions, cancellationToken);

        var links = new List<AcceptedAnswerDiscussion>();
        foreach (var page in pages)
        {
            var url = await UrlRetriever.Retrieve(page, cancellationToken);
            links.Add(new AcceptedAnswerDiscussion(
                url.RelativePath,
                page.BasicItemTitle,
                page.QAndAQuestionPageDateCreated));
        }

        return new([.. links.OrderByDescending(l => l.Date)]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        MemberAcceptedAnswerDiscussionsQuery query,
        MemberAcceptedAnswerDiscussionsQueryResponse result,
        ICacheDependencyKeysBuilder builder) =>
        builder
            .AllObjects(QAndAAnswerDataInfo.OBJECT_TYPE)
            .AllContentItems(QAndAQuestionPage.CONTENT_TYPE_NAME);
}
