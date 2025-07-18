using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerDataByIDQuery(int AnswerDataID) : IQuery<QAndAAnswerDataInfo>, ICacheByValueQuery
{
    public string CacheValueKey => AnswerDataID.ToString();
}

public class QAndAAnswerDataByIDQueryHandler(DataItemQueryTools tools, IInfoProvider<QAndAAnswerDataInfo> provider)
    : DataItemQueryHandler<QAndAAnswerDataByIDQuery, QAndAAnswerDataInfo>(tools)
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;

    public override async Task<QAndAAnswerDataInfo> Handle(QAndAAnswerDataByIDQuery request, CancellationToken cancellationToken = default) =>
        await provider.GetAsync(request.AnswerDataID);

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAAnswerDataByIDQuery query, QAndAAnswerDataInfo result, ICacheDependencyKeysBuilder builder) =>
        builder.Object(QAndAAnswerDataInfo.OBJECT_TYPE, query.AnswerDataID);
}
