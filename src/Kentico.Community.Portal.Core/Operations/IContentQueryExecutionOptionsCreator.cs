using CMS.Websites.Routing;
using Microsoft.AspNetCore.Http;

namespace Kentico.Community.Portal.Core;

public interface IContentQueryExecutionOptionsCreator
{
    public ContentQueryExecutionOptions Create();
}

public class DefaultContentQueryExecutionOptionsCreator(
    IWebsiteChannelContext websiteChannelContext,
    IHttpContextAccessor contextAccessor
) : IContentQueryExecutionOptionsCreator
{
    private readonly IWebsiteChannelContext websiteChannelContext = websiteChannelContext;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    public ContentQueryExecutionOptions Create()
    {
        if (contextAccessor.HttpContext is not HttpContext httpContext)
        {
            return new ContentQueryExecutionOptions { ForPreview = false, IncludeSecuredItems = false };
        }

        return new ContentQueryExecutionOptions
        {
            ForPreview = websiteChannelContext.IsPreview,
            IncludeSecuredItems = httpContext.User.Identities
                .TryFirst()
                .Map(i => i.IsAuthenticated)
                .GetValueOrDefault(false)
        };
    }
}
