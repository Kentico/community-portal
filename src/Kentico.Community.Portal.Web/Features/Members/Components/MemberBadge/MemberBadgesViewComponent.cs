using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Members;

public class MemberBadgesViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IReadOnlyList<MemberBadgeViewModel> badges) =>
        View("~/Features/Members/Components/MemberBadge/MemberBadges.cshtml", badges);
}

public class MemberBadgeViewModel
{
    public string? BadgeImageUrl { get; set; }
    public string MemberBadgeDisplayName { get; set; } = string.Empty;
    public string MemberBadgeDescription { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public int BadgeId { get; set; }
}
