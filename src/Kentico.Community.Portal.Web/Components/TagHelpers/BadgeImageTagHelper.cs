using Kentico.Community.Portal.Web.Features.Members;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_NAME, Attributes = ATTRIBUTE_NAME, TagStructure = TagStructure.WithoutEndTag)]
public class BadgeImageTagHelper : TagHelper
{
    public const string ELEMENT_NAME = "img";
    public const string ATTRIBUTE_NAME = "xpc-badge";

    [HtmlAttributeName(ATTRIBUTE_NAME)]
    public Maybe<MemberBadgeViewModel> Badge { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Badge.TryGetValue(out var badge))
        {
            output.SuppressOutput();

            return;
        }
        if (!badge.BadgeImageUrl.TryGetValue(out string? badgeURL))
        {
            output.SuppressOutput();

            return;
        }
        output.TagName = "img";

        output.Attributes.SetAttribute("src", badgeURL);
        output.Attributes.SetAttribute("class", "c-tag_badge align-text-top border border-1 rounded-circle");
        output.Attributes.SetAttribute("alt", $"{badge.MemberBadgeDisplayName} badge");
    }
}
