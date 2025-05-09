using Kentico.Community.Portal.Web.Infrastructure.Search;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_NAME, Attributes = ATTRIBUTE_NAME_FACET_FIELD)]
public class FacetFieldTagHelper : TagHelper
{
    public const string ELEMENT_NAME = "input";
    public const string ATTRIBUTE_NAME_FACET_FIELD = "xpc-facet-field";
    public const string ATTRIBUTE_NAME_MOBILE = "xpc-facet-field-mobile";

    [HtmlAttributeName(ATTRIBUTE_NAME_FACET_FIELD)]
    public FacetOption? Facet { get; set; }

    [HtmlAttributeName(ATTRIBUTE_NAME_MOBILE)]
    public bool? Mobile { get; set; } = false;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        _ = output.Attributes.RemoveAll(ATTRIBUTE_NAME_FACET_FIELD);

        if (Facet is null)
        {
            return;
        }

        output.Attributes
            .TryFirst(a => string.Equals(a.Name, "name", StringComparison.OrdinalIgnoreCase))
            .Execute(a =>
            {
                string id = !(Mobile ?? false)
                    ? $"{a.Value}-{Facet.Value}"
                    : $"{a.Value}M-{Facet.Value}";
                output.Attributes.Add("id", id);
            });

        output.Attributes.Add("facet-field", "");
        if (Mobile ?? false)
        {
            output.Attributes.Add("facet-field-mobile", "");
        }
        output.Attributes.Add("value", Facet.Value);

        if (Facet.Count == 0)
        {
            output.Attributes.Add("disabled", "");
        }

        if (Facet.IsSelected)
        {
            output.Attributes.Add("checked", "");
        }
    }
}
