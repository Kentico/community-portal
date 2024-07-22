using System.Collections.Immutable;
using CMS.DataEngine;
using CMS.Helpers;

namespace Kentico.Community.Portal.Core.Modules;

public interface IMemberBadgeInfoProvider : IInfoProvider<MemberBadgeInfo>
{
    Task<ImmutableList<MemberBadgeInfo>> GetAllMemberBadgesCached();
}

public class MemberBadgeInfoProvider(IProgressiveCache cache, IInfoProvider<MemberBadgeInfo> infoProvider) : IMemberBadgeInfoProvider
{
    private readonly IProgressiveCache cache = cache;
    private readonly IInfoProvider<MemberBadgeInfo> infoProvider = infoProvider;

    public ObjectQuery<MemberBadgeInfo> Get() => infoProvider.Get();
    public void Delete(MemberBadgeInfo info) => infoProvider.Delete(info);
    public void Set(MemberBadgeInfo info) => infoProvider.Set(info);

    public async Task<ImmutableList<MemberBadgeInfo>> GetAllMemberBadgesCached()
    {
        var badges = await cache.LoadAsync(cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberBadgeInfo.OBJECT_TYPE}|all");

            return infoProvider.Get().GetEnumerableTypedResultAsync();
        }, new CacheSettings(120, [nameof(MemberBadgeInfo), "all"]));

        return badges.ToImmutableList();
    }
}
