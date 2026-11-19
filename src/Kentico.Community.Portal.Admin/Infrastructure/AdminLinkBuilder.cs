using CMS.EmailEngine;
using Kentico.Xperience.Admin.Base;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Admin.Infrastructure;

/// <summary>
/// The single place that knows how a URL into the administration UI is assembled.
/// <para>
/// <see cref="IPageLinkGenerator" /> returns a path relative to the administration root, which is
/// what the admin UI's own client-side router wants but is wrong everywhere else - used as-is in an
/// email or an anchor it resolves against the site root and lands on a 404. Both methods here put
/// that root back, so no caller has to remember to.
/// </para>
/// </summary>
public class AdminLinkBuilder(
    IPageLinkGenerator pageLinkGenerator,
    IOptionsMonitor<SystemEmailOptions> systemEmailOptions)
{
    /// <summary>
    /// Where the administration UI is mounted. Xperience exposes no configuration API for this, so
    /// it is stated once here rather than spelled out at each call site.
    /// </summary>
    public const string ADMIN_ROOT_PATH = "admin";

    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;
    private readonly IOptionsMonitor<SystemEmailOptions> systemEmailOptions = systemEmailOptions;

    /// <summary>
    /// A site-relative path to an administration page, for links rendered inside the admin UI.
    /// </summary>
    public string GetPath<TPage>(PageParameterValues? parameters = null) where TPage : IPage =>
        $"/{ADMIN_ROOT_PATH}/{pageLinkGenerator.GetPath<TPage>(parameters).TrimStart('/')}";

    /// <summary>
    /// An absolute URL to an administration page, for links that leave the browser - emails above
    /// all, where a relative path has nothing to resolve against.
    /// </summary>
    /// <remarks>
    /// The host comes from the email service domain. That is the domain a recipient of a system
    /// email can actually reach, which is what these links are for. Falls back to the site-relative
    /// path when no domain is configured, so a misconfigured environment produces a link that is
    /// merely incomplete rather than one pointing at the wrong host.
    /// </remarks>
    public string GetAbsoluteUrl<TPage>(PageParameterValues? parameters = null) where TPage : IPage
    {
        string path = GetPath<TPage>(parameters);
        string domain = systemEmailOptions.CurrentValue.ServiceDomain ?? "";

        if (string.IsNullOrWhiteSpace(domain))
        {
            return path;
        }

        if (!domain.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            domain = $"https://{domain}";
        }

        return $"{domain.TrimEnd('/')}{path}";
    }
}
