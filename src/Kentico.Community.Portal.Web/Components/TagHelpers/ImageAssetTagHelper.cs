using Kentico.Community.Portal.Web.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_TAG, TagStructure = TagStructure.WithoutEndTag, Attributes = ATTRIBUTE_IMAGE)]
public class ImageAssetTagHelper : TagHelper
{
    public const string ELEMENT_TAG = "img";
    public const string ATTRIBUTE_IMAGE = "xpc-image-asset";

    [HtmlAttributeName(ATTRIBUTE_IMAGE)]
    public Maybe<ImageAssetViewModel> Image { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Image.TryGetValue(out var img))
        {
            output.SuppressOutput();

            return;
        }

        output.Attributes.SetAttribute("title", img.Title);
        output.Attributes.SetAttribute("alt", img.AltText);
        output.Attributes.SetAttribute("loading", "lazy");
    }
}
