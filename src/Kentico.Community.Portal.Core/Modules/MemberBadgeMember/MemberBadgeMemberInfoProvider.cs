using System.Collections.Frozen;
using System.Collections.Immutable;
using CMS.DataEngine;
using CMS.Helpers;

namespace Kentico.Community.Portal.Core.Modules;

public interface IMemberBadgeMemberInfoProvider : IInfoProvider<MemberBadgeMemberInfo>, IBulkInfoProvider<MemberBadgeMemberInfo>
{
    Task<MemberBadgeRelationshipDictionary> GetAllMemberBadgeRelationshipsCached();
    Task<ImmutableList<MemberBadgeMemberInfo>> GetAllMemberBadgesForMemberCached(int memberID);
}

public class MemberBadgeMemberInfoProvider(IProgressiveCache cache, IInfoProvider<MemberBadgeMemberInfo> infoProvider) : IMemberBadgeMemberInfoProvider
{
    private readonly IProgressiveCache cache = cache;
    private readonly IInfoProvider<MemberBadgeMemberInfo> infoProvider = infoProvider;

    public ObjectQuery<MemberBadgeMemberInfo> Get() => infoProvider.Get();
    public void Delete(MemberBadgeMemberInfo info) => infoProvider.Delete(info);
    public void Set(MemberBadgeMemberInfo info) => infoProvider.Set(info);

    public async Task<ImmutableList<MemberBadgeMemberInfo>> GetAllMemberBadgesForMemberCached(int memberID)
    {
        var badges = await cache.LoadAsync(cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberBadgeMemberInfo.OBJECT_TYPE}|all");

            return Get()
                .WhereEquals(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId), memberID)
                .GetEnumerableTypedResultAsync();
        }, new CacheSettings(120, [nameof(MemberBadgeMemberInfo), "bymemberid", memberID]));

        return badges.ToImmutableList();
    }

    public Task<MemberBadgeRelationshipDictionary> GetAllMemberBadgeRelationshipsCached() =>
        cache.LoadAsync(async cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberBadgeMemberInfo.OBJECT_TYPE}|all");

            var results = await Get()
                .Columns(
                    nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId),
                    nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberBadgeId),
                    nameof(MemberBadgeMemberInfo.MemberBadgeMemberIsSelected)
                )
                .GetEnumerableTypedResultAsync();

            var dict = results
                .Select(b => new MemberBadgeRelationship(b.MemberBadgeMemberMemberBadgeId, b.MemberBadgeMemberMemberId, b.MemberBadgeMemberIsSelected))
                .GroupBy(b => b.MemberID)
                .ToFrozenDictionary(b => b.Key, g => g.ToFrozenDictionary(g => g.BadgeID));

            return new MemberBadgeRelationshipDictionary(dict);
        }, new CacheSettings(120, [nameof(GetAllMemberBadgeRelationshipsCached)]));

    public void BulkInsert(IEnumerable<MemberBadgeMemberInfo> objects, BulkInsertSettings? settings = null) => infoProvider.BulkInsert(objects, settings);
    public void BulkUpdate(IEnumerable<KeyValuePair<string, object>> values, IWhereCondition where, bool useApi = false) => infoProvider.BulkUpdate(values, where, useApi);
    public void BulkUpdate(string updateExpression, QueryDataParameters updateParameters, IWhereCondition where) => infoProvider.BulkUpdate(updateExpression, updateParameters, where);
    public void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null) => infoProvider.BulkDelete(where, settings);
}

public record MemberBadgeRelationship(int BadgeID, int MemberID, bool IsSelected);

public class MemberBadgeRelationshipDictionary
{
    private readonly FrozenDictionary<int, FrozenDictionary<int, MemberBadgeRelationship>> dict;

    public MemberBadgeRelationshipDictionary(FrozenDictionary<int, FrozenDictionary<int, MemberBadgeRelationship>> dict) => this.dict = dict;

    public FrozenDictionary<int, MemberBadgeRelationship>? RelationshipsForMember(int memberID) => dict.GetValueOrDefault(memberID);

    public bool HasEntry(int memberID, int badgeID)
    {
        var relationships = dict.GetValueOrDefault(memberID);

        return relationships?.ContainsKey(badgeID) ?? false;
    }
}
