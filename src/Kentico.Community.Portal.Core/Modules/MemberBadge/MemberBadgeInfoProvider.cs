using CMS.DataEngine;
using CMS.Helpers;

namespace Kentico.Community.Portal.Core.Modules;

public interface IMemberBadgeInfoProvider : IInfoProvider<MemberBadgeInfo>
{
    public Task<IReadOnlyList<MemberBadgeInfo>> GetAllMemberBadgesCached();
}

public class MemberBadgeInfoProvider(IProgressiveCache cache, IInfoProvider<MemberBadgeInfo> infoProvider) : IMemberBadgeInfoProvider
{
    private readonly IProgressiveCache cache = cache;
    private readonly IInfoProvider<MemberBadgeInfo> infoProvider = infoProvider;

    public ObjectQuery<MemberBadgeInfo> Get() => infoProvider.Get();
    public void Delete(MemberBadgeInfo info) => infoProvider.Delete(info);
    public void Set(MemberBadgeInfo info) => infoProvider.Set(info);
    public Task DeleteAsync(MemberBadgeInfo info) => infoProvider.DeleteAsync(info);
    public Task SetAsync(MemberBadgeInfo info) => infoProvider.SetAsync(info);

    public async Task<IReadOnlyList<MemberBadgeInfo>> GetAllMemberBadgesCached()
    {
        var badges = await cache.LoadAsync(async cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberBadgeInfo.OBJECT_TYPE}|all");

            var infos = await infoProvider.Get().GetEnumerableTypedResultAsync();

            return infos.ToList().AsReadOnly();
        }, new CacheSettings(120, [nameof(MemberBadgeInfo), "all"]));

        return badges;
    }
}
