using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogLandingPageQuery(RoutedWebPage Page, string ChannelName) : WebPageRoutedQuery<BlogLandingPage>(Page), IChannelContentQuery;
public class BlogLandingPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<BlogLandingPageQuery, BlogLandingPage>(tools)
{
    public override async Task<BlogLandingPage> Handle(BlogLandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.ChannelName, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<BlogLandingPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
