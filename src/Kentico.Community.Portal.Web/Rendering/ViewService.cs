using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Slugify;

namespace Kentico.Community.Portal.Web.Rendering;

public class ViewService
{
    private readonly IHttpContextAccessor contextAccessor;
    private readonly IWebHostEnvironment env;
    private IRequestCultureFeature cultureFeature = null!;

    public ViewService(
        IHttpContextAccessor contextAccessor,
        ISlugHelper slugHelper,
        IWebHostEnvironment env)
    {
        this.contextAccessor = contextAccessor;
        SlugHelper = slugHelper;
        this.env = env;
    }

    public CultureInfo Culture
    {
        get
        {
            cultureFeature ??= contextAccessor.HttpContext!.Features.Get<IRequestCultureFeature>()!;

            return cultureFeature.RequestCulture.Culture;
        }
    }

    public ISlugHelper SlugHelper { get; }

    /// <summary>
    /// Determines if View caching is enabled for the current request
    /// </summary>
    /// <returns></returns>
    public bool CacheEnabled =>
        // Disable view caching locally since we are using .NET hot reload
        !env.IsDevelopment();
}
