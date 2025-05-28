using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerCountQuery(Guid QuestionWebPageItemGUID) : IQuery<int>, ICacheByValueQuery
{
    public string CacheValueKey => QuestionWebPageItemGUID.ToString();
}

public class QAndAAnswerCountQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<QAndAAnswerDataInfo> provider,
    IContentQueryExecutor queryExecutor) : DataItemQueryHandler<QAndAAnswerCountQuery, int>(tools)
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;
    private readonly IContentQueryExecutor queryExecutor = queryExecutor;

    public override async Task<int> Handle(QAndAAnswerCountQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME).WithWebPageData())
            .Parameters(q => q
                .Where(w => w.WhereEquals(nameof(QAndAQuestionPage.SystemFields.WebPageItemGUID), request.QuestionWebPageItemGUID))
                .Columns(nameof(QAndAQuestionPage.SystemFields.WebPageItemID)));

        var pages = await queryExecutor.GetMappedResult<QAndAQuestionPage>(b, cancellationToken: cancellationToken);

        if (pages.FirstOrDefault() is not QAndAQuestionPage page)
        {
            return 0;
        }

        return await provider.Get()
                .WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), page.SystemFields.WebPageItemID)
                .GetCountAsync(cancellationToken);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAAnswerCountQuery query, int result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(QAndAAnswerDataInfo.OBJECT_TYPE);
}
