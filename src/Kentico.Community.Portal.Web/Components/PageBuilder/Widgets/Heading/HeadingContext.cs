using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Slugify;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Heading;

public class HeadingContext(ISlugHelper slugHelper, IHttpContextAccessor httpContextAccessor) : IHeadingContext
{
    private readonly ISlugHelper slugHelper = slugHelper;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly Dictionary<string, int> slugCounts = [];

    public string GetUniqueSlug(string headingText)
    {
        string baseSlug = slugHelper.GenerateSlug(headingText);

        // Skip uniqueness logic in Page Builder edit mode since components render asynchronously
        if (httpContextAccessor.HttpContext?.Kentico().PageBuilder().EditMode is true)
        {
            return baseSlug;
        }

        if (!slugCounts.TryGetValue(baseSlug, out int count))
        {
            slugCounts[baseSlug] = 1;
            return baseSlug;
        }

        slugCounts[baseSlug] = count + 1;
        return $"{baseSlug}-{count + 1}";
    }
}
