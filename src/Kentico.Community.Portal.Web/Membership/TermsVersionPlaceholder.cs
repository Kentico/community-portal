using System.Text.Encodings.Web;

namespace Kentico.Community.Portal.Web.Membership;

/// <summary>
/// Lets the <c>/terms</c> page show <see cref="TermsOptions.CurrentVersion" /> instead of a typed
/// version, so the page and the version stored on members can't disagree. Write
/// <see cref="TOKEN" /> in a Rich Text widget; the widget view replaces it.
/// </summary>
public static class TermsVersionPlaceholder
{
    public const string TOKEN = "{{TermsVersion}}";

    public static string Apply(string? html, string version) =>
        html is null || !html.Contains(TOKEN, StringComparison.Ordinal)
            ? html ?? ""
            : html.Replace(TOKEN, HtmlEncoder.Default.Encode(version), StringComparison.Ordinal);
}
