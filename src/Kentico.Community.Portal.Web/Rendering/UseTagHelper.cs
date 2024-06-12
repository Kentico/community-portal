using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;use&gt; elements (inside &lt;svg&gt; elements) that supports file versioning.
/// </summary>
/// <remarks>
/// The tag helper won't process for cases with just the 'xlink:href' attribute.
/// </remarks>
/// <remarks>
/// Creates a new <see cref="UseTagHelper"/>.
/// </remarks>
/// <param name="fileVersionProvider">The <see cref="IFileVersionProvider"/>.</param>
/// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
/// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
[HtmlTargetElement(
    "use",
    Attributes = AppendVersionAttributeName + "," + XLinkAttributeName,
    TagStructure = TagStructure.WithoutEndTag)]
public class UseTagHelper(
    IFileVersionProvider fileVersionProvider,
    HtmlEncoder htmlEncoder,
    IUrlHelperFactory urlHelperFactory) : UrlResolutionTagHelper(urlHelperFactory, htmlEncoder)
{
    private const string AppendVersionAttributeName = "asp-append-version";
    private const string XLinkAttributeName = "xlink:href";

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Source of the Use.
    /// </summary>
    /// <remarks>
    /// Passed through to the generated HTML in all cases.
    /// </remarks>
    [HtmlAttributeName(XLinkAttributeName)]
    public string XLink { get; set; } = "";

    /// <summary>
    /// Value indicating if file version should be appended to the xlink urls.
    /// </summary>
    /// <remarks>
    /// If <c>true</c> then a query string "v" with the encoded content of the file is added.
    /// </remarks>
    [HtmlAttributeName(AppendVersionAttributeName)]
    public bool AppendVersion { get; set; }

    internal IFileVersionProvider FileVersionProvider { get; private set; } = fileVersionProvider;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        output.CopyHtmlAttribute(XLinkAttributeName, context);
        ProcessUrlAttribute(XLinkAttributeName, output);

        if (AppendVersion)
        {
            EnsureFileVersionProvider();

            // Retrieve the TagHelperOutput variation of the "xlink" attribute in case other TagHelpers in the
            // pipeline have touched the value. If the value is already encoded this UseTagHelper may
            // not function properly.
            XLink = output.Attributes[XLinkAttributeName].Value as string ?? "";

            output.Attributes.SetAttribute(XLinkAttributeName, FileVersionProvider.AddFileVersionToPath(ViewContext.HttpContext.Request.PathBase, XLink));
        }
    }

    private void EnsureFileVersionProvider() => FileVersionProvider ??= ViewContext.HttpContext.RequestServices.GetRequiredService<IFileVersionProvider>();
}
