using System.Text;
using System.Text.RegularExpressions;

namespace Kentico.Community.Portal.Web.Features.SEO;

/// <summary>
/// Content of <c>/llms.txt</c> (https://llmstxt.org): H1 site name, blockquote summary, H2 sections of link lists.
/// </summary>
/// <remarks>
/// Link URLs are site-relative or absolute. Relative URLs are resolved against the request origin in <see cref="ToMarkdown"/>,
/// so the cached document is host-independent.
/// </remarks>
public partial record LlmsTxtDocument(IReadOnlyList<LlmsTxtSection> Sections)
{
    /// <summary>
    /// Renders the llms.txt markdown. Empty sections are omitted.
    /// </summary>
    /// <param name="siteName">H1 title</param>
    /// <param name="summary">Blockquote summary</param>
    /// <param name="baseURL">Origin used to make site-relative links absolute, e.g. <c>https://community.kentico.com</c></param>
    public string ToMarkdown(string siteName, string summary, string baseURL)
    {
        var sb = new StringBuilder();
        string origin = baseURL.TrimEnd('/');

        _ = sb.Append("# ").Append(SingleLine(siteName)).Append('\n')
            .Append('\n')
            .Append("> ").Append(SingleLine(summary)).Append('\n');

        foreach (var section in Sections.Where(s => s.Links.Count > 0))
        {
            _ = sb.Append('\n')
                .Append("## ").Append(SingleLine(section.Title)).Append('\n')
                .Append('\n');

            foreach (var link in section.Links)
            {
                _ = sb.Append("- [").Append(EscapeLinkText(link.Title)).Append("](")
                    .Append(EscapeURL(link.URL.StartsWith('/') ? origin + link.URL : link.URL)).Append(')');

                string description = SingleLine(link.Description);
                if (description.Length > 0)
                {
                    _ = sb.Append(": ").Append(description);
                }

                _ = sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    private static string SingleLine(string value) => WhitespaceRegex().Replace(value ?? "", " ").Trim();

    private static string EscapeLinkText(string value) =>
        SingleLine(value).Replace("\\", "\\\\").Replace("[", "\\[").Replace("]", "\\]");

    // Keeps the markdown link destination intact
    private static string EscapeURL(string value) =>
        value.Trim().Replace(" ", "%20").Replace("(", "%28").Replace(")", "%29");

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

public record LlmsTxtSection(string Title, IReadOnlyList<LlmsTxtLink> Links);

/// <param name="Title">Link text</param>
/// <param name="URL">Site-relative (starting with <c>/</c>) or absolute URL</param>
/// <param name="Description">Optional short description, rendered after the link</param>
public record LlmsTxtLink(string Title, string URL, string Description);
