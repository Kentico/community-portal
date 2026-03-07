using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Members.Components.MemberProfileDisplay;

public class MemberProfileDisplayViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(CommunityMember profile)
    {
        var renderModel = new ProfileRenderModel
        {
            ProfileData = profile,
            RenderMode = DetermineRenderMode(profile.ModerationStatus),
            HidePersonalDetails = profile.ModerationStatus == ModerationStatuses.Spam
        };

        return View($"~/Features/Members/Components/MemberProfileDisplay/{renderModel.RenderMode}.cshtml", renderModel);
    }

    private static ProfileDisplayMode DetermineRenderMode(ModerationStatuses modStatus) =>
        modStatus switch
        {
            ModerationStatuses.Flagged => ProfileDisplayMode.Restricted,
            ModerationStatuses.Spam => ProfileDisplayMode.Limited,
            ModerationStatuses.Archived => ProfileDisplayMode.WithNotice,
            ModerationStatuses.None => ProfileDisplayMode.Standard,
            _ => ProfileDisplayMode.Standard
        };
}

public class ProfileRenderModel
{
    public required CommunityMember ProfileData { get; set; }
    public ProfileDisplayMode RenderMode { get; set; }
    public bool HidePersonalDetails { get; set; }
}

public enum ProfileDisplayMode
{
    Standard,
    WithNotice,
    Limited,
    Restricted
}
