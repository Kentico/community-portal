using Kentico.Community.Portal.Web.Features.Members;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_NAME, Attributes = ATTRIBUTE_NAME, TagStructure = TagStructure.WithoutEndTag)]
public class MemberAvatarTagHelper(IWebHostEnvironment webHostEnvironment, LinkGenerator linkGenerator, IHttpContextAccessor contextAccessor) : TagHelper
{
    public const string ELEMENT_NAME = "img";
    public const string ATTRIBUTE_NAME = "xpc-member-avatar";

    [HtmlAttributeName(ATTRIBUTE_NAME)]
    public int MemberID { get; set; }

    private readonly IWebHostEnvironment webHostEnvironment = webHostEnvironment;
    private readonly LinkGenerator linkGenerator = linkGenerator;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        string avatarURL = GetAvatarURL();
        output.Attributes.SetAttribute("src", avatarURL);
    }

    private string GetAvatarURL()
    {
        if (MemberID == 0)
        {
            return $"~/img/kentico.png";
        }

        return linkGenerator.GetPathByAction(
                contextAccessor.HttpContext!,
                nameof(MemberController.GetAvatarImage),
                "member",
                new { memberID = MemberID }) ?? "";
    }
}
