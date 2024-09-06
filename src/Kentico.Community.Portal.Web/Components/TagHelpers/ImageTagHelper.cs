using Kentico.Community.Portal.Web.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_TAG, TagStructure = TagStructure.WithoutEndTag, Attributes = ATTRIBUTE)]
public class ImageTagHelper : TagHelper
{
    public const string ELEMENT_TAG = "img";
    public const string ATTRIBUTE = "xpc-image";

    [HtmlAttributeName(ATTRIBUTE)]
    public Maybe<ImageViewModel> Image { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Image.TryGetValue(out var img))
        {
            output.SuppressOutput();

            return;
        }

        output.Attributes.SetAttribute("src", img.URL);
        output.Attributes.SetAttribute("title", img.Title);
        output.Attributes.SetAttribute("width", img.Width);
        output.Attributes.SetAttribute("height", img.Height);
        output.Attributes.SetAttribute("alt", img.AltText);
    }
}
