using System.Collections.Frozen;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Accounts;
using Kentico.Community.Portal.Web.Features.QAndA.Search;
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
            .Select(a => MemberBadgeViewModel.Create(a, true))
            .ToList();
    }

    public async Task<IReadOnlyList<MemberBadgeViewModel>> GetAllBadgesFor(int memberId)
    {
        var badgeInfos = await GetMemberBadgesWithMediaAssets();
        var badgeDtos = await GetAllBadgesForMember(memberId, badgeInfos, false);
        return badgeDtos
            .Select(MemberBadgeViewModel.Create)
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
                .Select(a => MemberBadgeViewModel.Create(a, true))
                .ToList();
        }

        return models;
    }

    public async Task UpdateSelectedBadgesFor(List<SelectedBadgeViewModel> badges, int memberId)
    {
        var badgesInDb = await memberBadgeMemberInfoProvider.Get()
            .WhereEquals(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId), memberId)
            .GetEnumerableTypedResultAsync();

        foreach (var badge in badges)
        {
            badgesInDb
                .TryFirst(x => x.MemberBadgeMemberMemberBadgeId == badge.BadgeId)
                .Execute(b =>
                {
                    b.MemberBadgeMemberIsSelected = badge.IsSelected;
                    b.Update();
                });
        }
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

            results.Add(new MemberBadgeAggregate(b.MemberBadge, b.Image, item.IsSelected));
        }

        return results;
    }

    private async Task<FrozenDictionary<int, MemberBadgetWithMediaAsset>> GetMemberBadgesWithMediaAssets()
    {
        var badges = await memberBadgeInfoProvider.GetAllMemberBadgesCached();
        var images = new Dictionary<Guid, ImageViewModel>();

        foreach (var imageGUID in badges.SelectMany(b => b.MemberBadgeImageContent.Select(i => i.Identifier).Where(id => id != default)))
        {
            // We have very few badges, so an N+1 query is better for cache busting and not a huge performance hit
            await mediator.Send(new ImageContentByGUIDQuery(imageGUID))
                .Execute(image => images.Add(imageGUID, ImageViewModel.Create(image)));
        }

        var resultsDictionary = new Dictionary<int, MemberBadgetWithMediaAsset>();

        foreach (var badge in badges)
        {
            var imageGUID = badge.MemberBadgeImageContent.Select(i => i.Identifier).FirstOrDefault();
            if (!images.TryGetValue(imageGUID, out var image))
            {
                continue;
            }

            resultsDictionary.Add(badge.MemberBadgeID, new MemberBadgetWithMediaAsset(badge, image));
        }

        return resultsDictionary.ToFrozenDictionary();
    }

    public record MemberBadgeAggregate(MemberBadgeInfo MemberBadge, ImageViewModel? Image, bool IsSelected);
    private record MemberBadgetWithMediaAsset(MemberBadgeInfo MemberBadge, ImageViewModel Image);
}

