using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Members;

public record MemberByIDQuery(int MemberID) : IQuery<MemberInfo?>, ICacheByValueQuery
{
    public string CacheValueKey => MemberID.ToString();
}

public class MemberByIDQueryHandler(DataItemQueryTools tools, IInfoProvider<MemberInfo> provider) : DataItemQueryHandler<MemberByIDQuery, MemberInfo?>(tools)
{
    private readonly IInfoProvider<MemberInfo> provider = provider;

    public override async Task<MemberInfo?> Handle(MemberByIDQuery request, CancellationToken cancellationToken)
    {
        var members = await provider.Get()
            .WhereEquals(nameof(MemberInfo.MemberID), request.MemberID)
            .GetEnumerableTypedResultAsync();

        return members.FirstOrDefault();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MemberByIDQuery query, MemberInfo? result, ICacheDependencyKeysBuilder builder) =>
        builder.Object(MemberInfo.OBJECT_TYPE, query.MemberID);
}
