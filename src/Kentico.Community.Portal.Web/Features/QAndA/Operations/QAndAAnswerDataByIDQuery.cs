using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerDataByIDQuery(int AnswerDataID) : IQuery<QAndAAnswerDataInfo>, ICacheByValueQuery
{
    public string CacheValueKey => AnswerDataID.ToString();
}

public class QAndAAnswerDataByIDQueryHandler : DataItemQueryHandler<QAndAAnswerDataByIDQuery, QAndAAnswerDataInfo>
{
    private readonly IQAndAAnswerDataInfoProvider provider;

    public QAndAAnswerDataByIDQueryHandler(DataItemQueryTools tools, IQAndAAnswerDataInfoProvider provider) : base(tools) => this.provider = provider;

    public override async Task<QAndAAnswerDataInfo> Handle(QAndAAnswerDataByIDQuery request, CancellationToken cancellationToken = default)
    {
        var item = await provider.GetAsync(request.AnswerDataID);

        return item;
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAAnswerDataByIDQuery query, QAndAAnswerDataInfo result, ICacheDependencyKeysBuilder builder) =>
        builder.Object(QAndAAnswerDataInfo.OBJECT_TYPE, query.AnswerDataID);
}
