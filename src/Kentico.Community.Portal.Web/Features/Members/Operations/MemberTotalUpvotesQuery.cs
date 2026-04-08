using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Members;

public record MemberTotalUpvotesResult(int AnswerUpvotes, int QuestionUpvotes)
{
    public int TotalUpvotes => AnswerUpvotes + QuestionUpvotes;
}

public record MemberTotalUpvotesQuery(int MemberID) : IQuery<MemberTotalUpvotesResult>, ICacheByValueQuery
{
    public string CacheValueKey => MemberID.ToString();
}

public class MemberTotalUpvotesQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<QAndAAnswerDataInfo> answerProvider,
    IInfoProvider<QAndAAnswerReactionInfo> answerReactionProvider,
    IInfoProvider<QAndAQuestionReactionInfo> questionReactionProvider,
    IContentQueryExecutor queryExecutor)
    : DataItemQueryHandler<MemberTotalUpvotesQuery, MemberTotalUpvotesResult>(tools)
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> answerProvider = answerProvider;
    private readonly IInfoProvider<QAndAAnswerReactionInfo> answerReactionProvider = answerReactionProvider;
    private readonly IInfoProvider<QAndAQuestionReactionInfo> questionReactionProvider = questionReactionProvider;
    private readonly IContentQueryExecutor queryExecutor = queryExecutor;

    public override async Task<MemberTotalUpvotesResult> Handle(MemberTotalUpvotesQuery request, CancellationToken cancellationToken = default)
    {
        // Count upvotes on the member's answers
        var memberAnswerIDs = (await answerProvider.Get()
            .WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID), request.MemberID)
            .Columns(nameof(QAndAAnswerDataInfo.QAndAAnswerDataID))
            .GetEnumerableTypedResultAsync())
            .Select(a => a.QAndAAnswerDataID)
            .ToList();

        int answerUpvotes = 0;
        if (memberAnswerIDs.Count > 0)
        {
            answerUpvotes = await answerReactionProvider.Get()
                .WhereIn(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionAnswerID), memberAnswerIDs)
                .GetCountAsync(cancellationToken);
        }

        // Count upvotes on the member's questions
        var questionBuilder = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME)
                .WithContentTypeFields()
                .WithWebPageData())
            .Parameters(q => q
                .Where(w => w.WhereEquals(nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID), request.MemberID))
                .Columns(nameof(QAndAQuestionPage.SystemFields.WebPageItemID)));

        var questionPages = await queryExecutor.GetMappedResult<QAndAQuestionPage>(questionBuilder, cancellationToken: cancellationToken);

        int questionUpvotes = 0;
        var questionPageItemIDs = questionPages.Select(p => p.SystemFields.WebPageItemID).ToList();
        if (questionPageItemIDs.Count > 0)
        {
            questionUpvotes = await questionReactionProvider.Get()
                .WhereIn(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionWebPageItemID), questionPageItemIDs)
                .GetCountAsync(cancellationToken);
        }

        return new MemberTotalUpvotesResult(answerUpvotes, questionUpvotes);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        MemberTotalUpvotesQuery query,
        MemberTotalUpvotesResult result,
        ICacheDependencyKeysBuilder builder) =>
        builder
            .AllObjects(QAndAAnswerDataInfo.OBJECT_TYPE)
            .AllObjects(QAndAAnswerReactionInfo.OBJECT_TYPE)
            .AllObjects(QAndAQuestionReactionInfo.OBJECT_TYPE)
            .AllContentItems(QAndAQuestionPage.CONTENT_TYPE_NAME);
}
