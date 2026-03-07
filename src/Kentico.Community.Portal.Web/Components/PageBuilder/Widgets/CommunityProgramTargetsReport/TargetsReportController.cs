using Kentico.Community.Portal.Web.Components.TagHelpers;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport;

[Route("[controller]/[action]")]
public class TargetsReportController(
    UserManager<CommunityMember> userManager,
    IWidgetPropertiesSerializer serializer,
    IWebPageDataContextRetriever contextRetriever) : Controller
{
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenderWidget([FromForm] int? emulatedMemberID)
    {
        var currentMember = await userManager.GetUserAsync(User);
        if (currentMember?.IsInternalEmployee != true)
        {
            return Forbid("Operation not permitted");
        }

        var props = HttpContext.GetWidgetProperties<TargetsReportWidgetProperties>(serializer);
        if (props is null || !contextRetriever.TryRetrieve(out var data))
        {
            return BadRequest();
        }

        props.EmulatedMemberID = emulatedMemberID;
        return ViewComponent(typeof(TargetsReportWidget), ComponentViewModel<TargetsReportWidgetProperties>.Create(data.WebPage, props));
    }
}
