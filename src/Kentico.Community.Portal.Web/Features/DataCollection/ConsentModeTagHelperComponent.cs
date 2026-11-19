using CMS.Websites.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Puts the Consent Mode default (and the saved choice) at the very top of <c>&lt;head&gt;</c>.
/// </summary>
/// <remarks>
/// Xperience Tag Manager renders HeadTop snippets (prod's GTM snippet) into the head's pre-content
/// from its own tag helper component (<c>Order</c> 1), ahead of anything in the layout. This
/// component has a lower <see cref="Order"/>, so its script comes first and <c>consent default</c>
/// is in <c>dataLayer</c> before GTM loads. Same channel condition as Tag Manager.
/// </remarks>
public class ConsentModeTagHelperComponent(
    CookieConsentManager cookieConsentManager,
    IWebsiteChannelContext websiteChannelContext) : TagHelperComponent
{
    private readonly CookieConsentManager cookieConsentManager = cookieConsentManager;
    private readonly IWebsiteChannelContext websiteChannelContext = websiteChannelContext;

    /// <summary>Before Tag Manager's <c>CodeSnippetTagHelperComponent</c> (1).</summary>
    public override int Order => -1000;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!string.Equals(context.TagName, "head", StringComparison.OrdinalIgnoreCase)
            || websiteChannelContext.WebsiteChannelID <= 0)
        {
            return;
        }

        output.PreContent.AppendHtml(ConsentModeScript.ForPageLoad(cookieConsentManager.GetChoice()));
    }
}
