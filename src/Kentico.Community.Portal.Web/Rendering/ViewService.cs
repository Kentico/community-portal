using System.Globalization;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Slugify;

namespace Kentico.Community.Portal.Web.Rendering;

public class ViewService(
    ISlugHelper slugHelper,
    IWebHostEnvironment env,
    IHttpContextAccessor contextAccessor)
{
    private readonly IWebHostEnvironment env = env;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    public CultureInfo Culture => CultureInfo.CurrentUICulture;

    public ISlugHelper SlugHelper { get; } = slugHelper;

    public PageBuilderMode PageBuilderMode
    {
        get
        {
            var ctx = contextAccessor.HttpContext;

            if (ctx.Kentico().PageBuilder().EditMode)
            {
                return PageBuilderMode.Edit;
            }

            if (ctx.Kentico().Preview().Enabled)
            {
                return PageBuilderMode.Preview;
            }

            return PageBuilderMode.Live;
        }
    }

    /// <summary>
    /// Determines if View caching is enabled for the current request
    /// </summary>
    /// <returns></returns>
    public bool CacheEnabled =>
        // Disable view caching locally since we are using .NET hot reload
        !env.IsDevelopment();
}

public enum PageBuilderMode
{
    Live,
    Preview,
    Edit
}
