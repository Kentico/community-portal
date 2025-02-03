using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_NAME)]
public class AlertTagHelper : TagHelper
{
    public const string ELEMENT_NAME = "xpc-alert";

    public bool Dismissable { get; set; } = false;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var childContent = await output.GetChildContentAsync();
        string alertClasses = Dismissable ? "alert-dismissible fade show" : "";

        output.TagName = "div";
        output.Attributes.SetAttribute("class", $"alert alert-secondary my-0 mt-4 {alertClasses}");
        output.Attributes.SetAttribute("role", "alert");
        _ = output.Content.SetHtmlContent($"<div>{childContent.GetContent()}</div>");

        if (Dismissable)
        {
            _ = output.Content.AppendHtml("<button type='button' class='btn-close' data-bs-dismiss='alert' aria-label='Close'></button>");
        }
    }
}
