using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerDatasByQuestionQuery(int QuestionWebPageItemID) : IQuery<QAndAAnswerDatasByQuestionQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => QuestionWebPageItemID.ToString();
}
public record QAndAAnswerDatasByQuestionQueryResponse(IReadOnlyList<QAndAAnswerDataInfo> Items);
public class QAndAAnswerDatasByQuestionQueryHandler(DataItemQueryTools tools, IQAndAAnswerDataInfoProvider provider) : DataItemQueryHandler<QAndAAnswerDatasByQuestionQuery, QAndAAnswerDatasByQuestionQueryResponse>(tools)
{
    private readonly IQAndAAnswerDataInfoProvider provider = provider;

    public override async Task<QAndAAnswerDatasByQuestionQueryResponse> Handle(QAndAAnswerDatasByQuestionQuery request, CancellationToken cancellationToken)
    {
        var items = await provider.Get()
            .WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), request.QuestionWebPageItemID)
            .GetEnumerableTypedResultAsync();

        return new(items.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAAnswerDatasByQuestionQuery query, QAndAAnswerDatasByQuestionQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(QAndAAnswerDataInfo.OBJECT_TYPE);
}
