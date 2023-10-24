using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

/// <summary>
/// Displays the inner content of the Tag Helper only when the page is being viewed
/// in Page Builder or Preview mode
/// </summary>
[HtmlTargetElement("xpc-page-builder-content", TagStructure = TagStructure.NormalOrSelfClosing)]
public class PageBuilderContentTagHelper : TagHelper
{
    private readonly IWebsiteChannelContext channelContext;

    public PageBuilderContentTagHelper(IWebsiteChannelContext channelContext) => this.channelContext = channelContext;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!channelContext.IsPreview)
        {
            output.SuppressOutput();
        }

        output.TagName = "";
    }
}

/// <summary>
/// Displays a helpful message that a Widget needs to be configured when the page is viewed
/// in Page Builder or Preview mode.
/// </summary>
[HtmlTargetElement("xpc-misconfigured-widget-instructions", TagStructure = TagStructure.WithoutEndTag)]
public class MisconfiguredWidgetInstructionsTagHelper : TagHelper
{
    private readonly IHttpContextAccessor accessor;

    public MisconfiguredWidgetInstructionsTagHelper(IHttpContextAccessor accessor) => this.accessor = accessor;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var httpContext = accessor.HttpContext;

        if (!httpContext.Kentico().PageBuilder().EditMode && !httpContext.Kentico().Preview().Enabled)
        {
            output.SuppressOutput();
        }

        output.TagName = "";

        _ = httpContext.Kentico().PageBuilder().EditMode
            ? output.Content.AppendHtml($"<p class=\"m-5\">This widget needs some setup. Open the <strong>widget properties</strong> to configure content and design for this widget.</p>")
            : output.Content.AppendHtml($"<p class=\"m-5\">This widget needs some setup. Switch to the <strong>Page Builder</strong> and then the <strong>widget properties</strong> to configure content and design for this widget.</p>");
    }
}
