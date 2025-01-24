using Microsoft.AspNetCore.Mvc;
using static Kentico.Community.Portal.Web.Features.Members.Badges.MemberBadgeService;

namespace Kentico.Community.Portal.Web.Features.Members;

public class MemberBadgesViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(
        IReadOnlyList<MemberBadgeViewModel> badges,
        BadgeDisplayStyle displayStyle = BadgeDisplayStyle.IconOnly) =>
        View("~/Features/Members/Components/MemberBadge/MemberBadges.cshtml", new MemberBadgesViewModel(badges, displayStyle));
}

public enum BadgeDisplayStyle
{
    IconOnly,
    Full
}

public class MemberBadgeViewModel
{
    public Maybe<string> BadgeImageUrl { get; }
    public string MemberBadgeDisplayName { get; } = string.Empty;
    public string MemberBadgeCodeName { get; } = string.Empty;
    public string MemberBadgeDescription { get; } = string.Empty;
    public bool IsSelected { get; init; }
    public int BadgeId { get; }

    public static MemberBadgeViewModel Create(MemberBadgeAggregate aggregate, bool isSelected) =>
        new(aggregate)
        {
            IsSelected = isSelected
        };
    public static MemberBadgeViewModel Create(MemberBadgeAggregate aggregate) =>
        new(aggregate);
    private MemberBadgeViewModel(MemberBadgeAggregate aggregate)
    {
        BadgeId = aggregate.MemberBadge.MemberBadgeID;
        MemberBadgeDisplayName = aggregate.MemberBadge.MemberBadgeDisplayName;
        MemberBadgeCodeName = aggregate.MemberBadge.MemberBadgeCodeName;
        MemberBadgeDescription = aggregate.MemberBadge.MemberBadgeShortDescription;
        BadgeImageUrl = Maybe.From(aggregate.Image).Map(i => i?.URL!).MapNullOrWhiteSpaceAsNone();
        IsSelected = aggregate.IsSelected;
    }
}

public record MemberBadgesViewModel(IReadOnlyList<MemberBadgeViewModel> Badges, BadgeDisplayStyle DisplayStyle);
