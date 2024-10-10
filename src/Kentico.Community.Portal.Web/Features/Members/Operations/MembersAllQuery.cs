using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Members;

public record MembersAllQuery : IQuery<Dictionary<int, MemberInfo>>;

public class MembersAllQueryHandler(DataItemQueryTools tools, IInfoProvider<MemberInfo> provider) : DataItemQueryHandler<MembersAllQuery, Dictionary<int, MemberInfo>>(tools)
{
    private readonly IInfoProvider<MemberInfo> provider = provider;

    public override async Task<Dictionary<int, MemberInfo>> Handle(MembersAllQuery request, CancellationToken cancellationToken)
    {
        var members = await provider.Get().GetEnumerableTypedResultAsync();

        return members.ToDictionary(m => m.MemberID);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MembersAllQuery query, Dictionary<int, MemberInfo> result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(MemberInfo.OBJECT_TYPE);
}
