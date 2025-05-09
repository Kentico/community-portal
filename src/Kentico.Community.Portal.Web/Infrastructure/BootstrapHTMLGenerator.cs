using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Custom HTML Generator to override the default behaviour and replace the CSS classes as they emerge.
/// Substitutes the default ASP.NET Core classes with Bootstrap 5.3 classes https://getbootstrap.com/docs/5.3/forms/validation/#how-it-works
/// </summary>
/// <remarks>
/// https://gist.github.com/FWest98/9141b3c2f260bee0e46058897d2017d2
/// </remarks>
public class BootstrapHTMLGenerator(IAntiforgery antiforgery, IOptions<MvcViewOptions> optionsAccessor,
    IModelMetadataProvider metadataProvider, IUrlHelperFactory urlHelperFactory, HtmlEncoder htmlEncoder,
    ValidationHtmlAttributeProvider validationAttributeProvider)
    : DefaultHtmlGenerator(antiforgery, optionsAccessor, metadataProvider, urlHelperFactory, htmlEncoder, validationAttributeProvider)
{
    protected override TagBuilder GenerateInput(ViewContext viewContext, InputType inputType, ModelExplorer modelExplorer, string expression,
        object value, bool useViewData, bool isChecked, bool setId, bool isExplicitValue, string format,
        IDictionary<string, object> htmlAttributes)
    {
        var tagBuilder = base.GenerateInput(viewContext, inputType, modelExplorer, expression, value, useViewData, isChecked, setId, isExplicitValue, format, htmlAttributes);
        FixValidationCssClassNames(tagBuilder);

        return tagBuilder;
    }

    public override TagBuilder GenerateTextArea(ViewContext viewContext, ModelExplorer modelExplorer, string expression, int rows,
        int columns, object htmlAttributes)
    {
        var tagBuilder = base.GenerateTextArea(viewContext, modelExplorer, expression, rows, columns, htmlAttributes);
        FixValidationCssClassNames(tagBuilder);

        return tagBuilder;
    }

    public override TagBuilder GenerateValidationMessage(ViewContext viewContext, ModelExplorer modelExplorer, string expression,
        string message, string tag, object htmlAttributes)
    {
        var tagBuilder = base.GenerateValidationMessage(viewContext, modelExplorer, expression, message, tag, htmlAttributes);
        FixValidationCssClassNames(tagBuilder);

        return tagBuilder;
    }

    public override TagBuilder GenerateValidationSummary(ViewContext viewContext, bool excludePropertyErrors, string message,
        string headerTag, object htmlAttributes)
    {
        var tagBuilder = base.GenerateValidationSummary(viewContext, excludePropertyErrors, message, headerTag, htmlAttributes);
        FixValidationCssClassNames(tagBuilder);

        return tagBuilder;
    }

    private static void FixValidationCssClassNames(TagBuilder? tagBuilder)
    {
        if (tagBuilder is null)
        {
            return;
        }

        tagBuilder.ReplaceCssClass(HtmlHelper.ValidationInputCssClassName, "is-invalid");
        tagBuilder.ReplaceCssClass(HtmlHelper.ValidationInputValidCssClassName, "is-valid");
        tagBuilder.ReplaceCssClass(HtmlHelper.ValidationMessageCssClassName, "is-invalid");
        tagBuilder.ReplaceCssClass(HtmlHelper.ValidationMessageValidCssClassName, "is-valid");
        tagBuilder.ReplaceCssClass(HtmlHelper.ValidationSummaryCssClassName, "is-invalid");
        tagBuilder.ReplaceCssClass(HtmlHelper.ValidationSummaryValidCssClassName, "is-valid");
    }
}

public static class TagBuilderHelpers
{
    public static void ReplaceCssClass(this TagBuilder tagBuilder, string old, string val)
    {
        if (!tagBuilder.Attributes.TryGetValue("class", out string? str) || string.IsNullOrWhiteSpace(str))
        {
            return;
        }

        tagBuilder.Attributes["class"] = str.Replace(old, val);
    }
}
