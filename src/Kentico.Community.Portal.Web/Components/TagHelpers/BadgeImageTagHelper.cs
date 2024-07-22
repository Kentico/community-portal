using Kentico.Community.Portal.Web.Features.Members;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement("badge-image", TagStructure = TagStructure.WithoutEndTag)]
public class BadgeImageTagHelper : TagHelper
{
    public MemberBadgeViewModel? Badge { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Badge is null)
        {
            output.SuppressOutput();

            return;
        }
        output.TagName = "img";

        output.Attributes.SetAttribute("src", Badge.BadgeImageUrl);
        output.Attributes.SetAttribute("class", "c-tag_badge align-text-top border border-1 rounded-circle");
        output.Attributes.SetAttribute("alt", $"{Badge.MemberBadgeDisplayName} badge");
    }
}
