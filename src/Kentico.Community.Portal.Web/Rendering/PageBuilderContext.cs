using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;

namespace Kentico.Community.Portal.Web.Rendering;


public enum ApplicationPageBuilderMode
{
    Live,
    Preview,
    Edit
}

public interface IPageBuilderContext
{
    public ApplicationPageBuilderMode Mode { get; }
}

public class PageBuilderContext(IHttpContextAccessor contextAccessor) : IPageBuilderContext
{
    public ApplicationPageBuilderMode Mode
    {
        get
        {
            var ctx = contextAccessor.HttpContext;

            if (ctx is null)
            {
                return ApplicationPageBuilderMode.Live;
            }

            if (ctx.Kentico().PageBuilder().EditMode)
            {
                return ApplicationPageBuilderMode.Edit;
            }

            if (ctx.Kentico().Preview().Enabled)
            {
                return ApplicationPageBuilderMode.Preview;
            }

            return ApplicationPageBuilderMode.Live;
        }
    }
}
