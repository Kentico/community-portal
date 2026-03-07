using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

/// <summary>
/// Creates a hidden input of the <see cref="Properties"/> value serialized and data protected
/// to support widget re-rendering and server round-trips
/// </summary>
/// <remarks>
/// Use with HttpContext.GetWidgetProperties() to retrieve the properties from the form submission
/// </remarks>
/// <param name="serializer"></param>
[HtmlTargetElement(ELEMENT_NAME)]
public class WidgetPropertiesTagHelper(IWidgetPropertiesSerializer serializer) : TagHelper
{
    public const string ELEMENT_NAME = "xpc-widget-properties";

    public IWidgetProperties? Properties { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Properties is null)
        {
            output.SuppressOutput();
        }

        output.Attributes.Clear();

        string value = serializer.Protect(Properties!);

        output.TagName = "input";
        output.TagMode = TagMode.SelfClosing;
        string? nameAttr = context.AllAttributes
            .TryFirst(a => string.Equals(a.Name, "name", StringComparison.OrdinalIgnoreCase))
            .Map(a => a.Value as string)
            .GetValueOrDefault("__ComponentProperties");
        output.Attributes.Add("name", nameAttr);
        output.Attributes.Add("type", "hidden");
        output.Attributes.Add("value", value);
    }
}

public static class HttpContextWidgetPropertiesExtensions
{
    public static T? GetWidgetProperties<T>(this HttpContext context, IWidgetPropertiesSerializer serializer) where T : IWidgetProperties =>
        GetWidgetProperties<T>(context, serializer, "__ComponentProperties");

    public static T? GetWidgetProperties<T>(this HttpContext context, IWidgetPropertiesSerializer serializer, string key) where T : IWidgetProperties
    {
        var val = context.Request.Form[key];

        string? str = val.FirstOrDefault();

        return str is null
            ? default
            : serializer.Unprotect<T>(str);
    }
}
