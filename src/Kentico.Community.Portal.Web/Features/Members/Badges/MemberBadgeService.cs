using System.Collections.Frozen;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Accounts;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Rendering;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class MemberBadgeService(
    IMemberBadgeInfoProvider memberBadgeInfoProvider,
    IMemberBadgeMemberInfoProvider memberBadgeMemberInfoProvider,
    IMediator mediator)
{
    private readonly IMemberBadgeInfoProvider memberBadgeInfoProvider = memberBadgeInfoProvider;
    private readonly IMemberBadgeMemberInfoProvider memberBadgeMemberInfoProvider = memberBadgeMemberInfoProvider;
    private readonly IMediator mediator = mediator;

    public async Task<IReadOnlyList<MemberBadgeViewModel>> GetSelectedBadgesFor(int memberId)
    {
        var badgeInfos = await GetMemberBadgesWithMediaAssets();
        var badgeDtos = await GetAllBadgesForMember(memberId, badgeInfos, true);
        return badgeDtos
            .Select(a => new MemberBadgeViewModel
            {
                BadgeId = a.MemberBadge.MemberBadgeID,
                MemberBadgeDisplayName = a.MemberBadge.MemberBadgeDisplayName,
                MemberBadgeDescription = a.MemberBadge.MemberBadgeShortDescription,
                BadgeImageUrl = a.MediaAsset?.MediaAssetContentAssetLight.Url,
                IsSelected = true,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<MemberBadgeViewModel>> GetAllBadgesFor(int memberId)
    {
        var badgeInfos = await GetMemberBadgesWithMediaAssets();
        var badgeDtos = await GetAllBadgesForMember(memberId, badgeInfos, false);
        return badgeDtos
            .Select(a => new MemberBadgeViewModel
            {
                BadgeId = a.MemberBadge.MemberBadgeID,
                MemberBadgeDisplayName = a.MemberBadge.MemberBadgeDisplayName,
                MemberBadgeDescription = a.MemberBadge.MemberBadgeShortDescription,
                BadgeImageUrl = a.MediaAsset?.MediaAssetContentAssetLight.Url,
                IsSelected = a.IsSelected,
            })
            .ToList();
    }

    public async Task<List<QAndAPostViewModel>> AddSelectedBadgesToQAndA(List<QAndAPostViewModel> models)
    {
        var authorIds = models.Select(x => x.Author.ID).DistinctBy(x => x);
        var badgeInfos = await GetMemberBadgesWithMediaAssets();

        foreach (var model in models)
        {
            var badgeDtos = await GetAllBadgesForMember(model.Author.ID, badgeInfos, true);
            model.Author.SelectedBadges = badgeDtos
                .Select(a => new MemberBadgeViewModel
                {
                    BadgeId = a.MemberBadge.MemberBadgeID,
                    MemberBadgeDisplayName = a.MemberBadge.MemberBadgeDisplayName,
                    MemberBadgeDescription = a.MemberBadge.MemberBadgeShortDescription,
                    BadgeImageUrl = a.MediaAsset?.MediaAssetContentAssetLight.Url,
                    IsSelected = true,
                })
                .ToList();
        }

        return models;
    }

    public async Task<bool> UpdateSelectedBadgesFor(List<SelectedBadgeViewModel> badges, int memberId)
    {
        var badgesInDb = await memberBadgeMemberInfoProvider.Get()
            .WhereEquals(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId), memberId)
            .GetEnumerableTypedResultAsync();

        foreach (var badge in badges)
        {
            var badgeToUpdate = badgesInDb.Single(x => x.MemberBadgeMemberMemberBadgeId == badge.BadgeId);
            badgeToUpdate.MemberBadgeMemberIsSelected = badge.IsSelected;
            badgeToUpdate.Update();
        }

        return true;
    }

    private async Task<IReadOnlyList<MemberBadgeAggregate>> GetAllBadgesForMember(int memberID, FrozenDictionary<int, MemberBadgetWithMediaAsset> badgeInfos, bool returnOnlySelected)
    {
        var memberBadges = await memberBadgeMemberInfoProvider.GetAllMemberBadgeRelationshipsCached();

        var results = new List<MemberBadgeAggregate>();

        if (memberBadges.RelationshipsForMember(memberID) is not { } ownedBadges)
        {
            return results;
        }

        foreach (var item in ownedBadges.Values.Where(b => !returnOnlySelected || b.IsSelected))
        {
            if (!badgeInfos.TryGetValue(item.BadgeID, out var b))
            {
                continue;
            }

            results.Add(new MemberBadgeAggregate(b.MemberBadge, b.MediaAsset, item.IsSelected));
        }

        return results;
    }

    private async Task<FrozenDictionary<int, MemberBadgetWithMediaAsset>> GetMemberBadgesWithMediaAssets()
    {
        var badges = await memberBadgeInfoProvider.GetAllMemberBadgesCached();
        var mediaAssets = new Dictionary<Guid, MediaAssetContent>();

        foreach (var mediaAssetGUID in badges.SelectMany(b => b.MemberBadgeMediaAssetContentItem.Select(i => i.Identifier)))
        {
            // We have very few badges, so an N+1 query is better for cache busting and not a huge performance hit
            var mediaAsset = await mediator.Send(new MediaAssetContentByGUIDQuery(mediaAssetGUID));
            mediaAssets.Add(mediaAssetGUID, mediaAsset);
        }

        var resultsDictionary = new Dictionary<int, MemberBadgetWithMediaAsset>();

        foreach (var badge in badges)
        {
            var contentItemGUID = badge.MemberBadgeMediaAssetContentItem.Select(i => i.Identifier).FirstOrDefault();

            if (!mediaAssets.TryGetValue(contentItemGUID, out var mediaAsset))
            {
                continue;
            }

            resultsDictionary.Add(badge.MemberBadgeID, new MemberBadgetWithMediaAsset(badge, mediaAsset));
        }

        return resultsDictionary.ToFrozenDictionary();
    }

    public class MemberBadgeAggregate(MemberBadgeInfo memberBadgeInfo, MediaAssetContent mediaAsset, bool isSelected)
    {
        public MemberBadgeInfo MemberBadge { get; set; } = memberBadgeInfo;
        public MediaAssetContent? MediaAsset { get; set; } = mediaAsset;
        public bool IsSelected { get; set; } = isSelected;
    }

    private record MemberBadgetWithMediaAsset(MemberBadgeInfo MemberBadge, MediaAssetContent MediaAsset);
}

