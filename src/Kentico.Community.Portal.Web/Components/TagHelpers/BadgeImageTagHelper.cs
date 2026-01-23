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

        string borderColor = badge.AlwaysSelected ? "var(--bs-secondary);" : "var(--bs-gray-600)";
        string borderWidth = badge.AlwaysSelected ? "border-2" : "border-1";

        output.Attributes.SetAttribute("src", badgeURL);
        output.Attributes.SetAttribute("class", $"align-text-top border {borderWidth} rounded-2 overflow-hidden");
        output.Attributes.SetAttribute("style", $"height: 1.5rem; width: 1.5rem; --bs-border-color: {borderColor}");
        output.Attributes.SetAttribute("alt", $"{badge.MemberBadgeDisplayName} badge");
    }
}
