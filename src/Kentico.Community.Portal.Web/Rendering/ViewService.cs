using System.Globalization;
using Slugify;

namespace Kentico.Community.Portal.Web.Rendering;

public class ViewService(
    ISlugHelper slugHelper,
    IWebHostEnvironment env)
{
    private readonly IWebHostEnvironment env = env;

    public CultureInfo Culture => CultureInfo.CurrentUICulture;

    public ISlugHelper SlugHelper { get; } = slugHelper;

    /// <summary>
    /// Determines if View caching is enabled for the current request
    /// </summary>
    /// <returns></returns>
    public bool CacheEnabled =>
        // Disable view caching locally since we are using .NET hot reload
        !env.IsDevelopment();
}
