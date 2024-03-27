using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.SEO;

public record RSSFeedPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<RSSFeedPage>(Page);
public class RSSFeedPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<RSSFeedPageQuery, RSSFeedPage>(tools)
{
    public override async Task<RSSFeedPage> Handle(RSSFeedPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page);

        var r = await Executor.GetMappedWebPageResult<RSSFeedPage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
