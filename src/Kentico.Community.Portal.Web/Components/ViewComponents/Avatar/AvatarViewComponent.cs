using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Avatar;

public class AvatarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(int memberID, AvatarSize size = AvatarSize.Normal) =>
        View("~/Components/ViewComponents/Avatar/Avatar.cshtml", new AvatarViewModel(size, memberID));
}

public record AvatarViewModel(AvatarSize Size, int MemberID);

public enum AvatarSize
{
    Small,
    Normal
}
