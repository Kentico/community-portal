using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_NAME, Attributes = ATTRIBUTE_NAME)]
public class MyButtonTagHelper : TagHelper
{
    public const string ELEMENT_NAME = "button";
    public const string ATTRIBUTE_NAME = "xpc-loading-button";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var childContent = await output.GetChildContentAsync();
        string originalContent = childContent.GetContent();

        string transformedContent = $@"
                <span default-content>
                    {originalContent}
                </span>
                <span loading-content>
                    <span>Loading...</span>
                    <span class='spinner-border spinner-border-sm' aria-hidden='true'></span>
                </span>
            ";

        _ = output.Content.SetHtmlContent(transformedContent);
        _ = output.Attributes.RemoveAll(ATTRIBUTE_NAME);
    }
}
