using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_NAME, Attributes = ATTRIBUTE_NAME, TagStructure = TagStructure.WithoutEndTag)]
public class MemberAvatarTagHelper(AvatarImageService avatarImageService, LinkGenerator linkGenerator, IHttpContextAccessor contextAccessor) : TagHelper
{
    public const string ELEMENT_NAME = "img";
    public const string ATTRIBUTE_NAME = "xpc-member-avatar";

    [HtmlAttributeName(ATTRIBUTE_NAME)]
    public int MemberID { get; set; }

    private readonly AvatarImageService avatarImageService = avatarImageService;
    private readonly LinkGenerator linkGenerator = linkGenerator;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        string avatarURL = await GetAvatarURL();
        output.Attributes.SetAttribute("src", avatarURL);
    }

    private async Task<string> GetAvatarURL()
    {
        if (MemberID == 0)
        {
            return $"~/img/kentico.webp";
        }

        var avatar = await avatarImageService.GetAvatarImage(MemberID);
        if (avatar.HasContentHubAvatar)
        {
            return avatar.ContentHubAvatarUrl!;
        }

        return linkGenerator.GetPathByAction(
                contextAccessor.HttpContext!,
                nameof(MemberController.GetAvatarImage),
                "member",
                new { memberID = MemberID }) ?? "";
    }
}
