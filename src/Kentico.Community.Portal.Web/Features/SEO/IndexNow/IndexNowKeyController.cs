using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.SEO.IndexNow;

/// <summary>
/// Serves the IndexNow verification key file at <c>/{key}.txt</c> (UTF-8 text, body is the key).
/// Every other <c>*.txt</c> name 404s.
/// </summary>
[Route("")]
public class IndexNowKeyController(IOptionsMonitor<IndexNowOptions> options) : Controller
{
    private readonly IOptionsMonitor<IndexNowOptions> options = options;

    [HttpGet("{key}.txt")]
    public IActionResult KeyFile(string key)
    {
        var settings = options.CurrentValue;

        return settings.IsEnabled && string.Equals(key, settings.Key, StringComparison.Ordinal)
            ? Content(settings.Key, "text/plain", Encoding.UTF8)
            : NotFound();
    }
}
