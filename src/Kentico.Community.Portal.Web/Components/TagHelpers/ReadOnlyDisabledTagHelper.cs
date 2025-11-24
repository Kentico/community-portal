using CMS.Base;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement("fieldset", Attributes = ATTRIBUTE_NAME)]
public class ReadOnlyDisabledTagHelper(IReadOnlyModeProvider readOnlyModeProvider) : TagHelper
{
    public const string ATTRIBUTE_NAME = "xpc-readonly-disabled";

    private readonly IReadOnlyModeProvider readOnlyModeProvider = readOnlyModeProvider;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        _ = output.Attributes.RemoveAll(ATTRIBUTE_NAME);

        if (readOnlyModeProvider.IsReadOnly)
        {
            output.Attributes.SetAttribute("disabled", "disabled");
        }
    }
}
