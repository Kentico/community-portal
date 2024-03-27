using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Home;

public record HomePageQuery(RoutedWebPage Page) : WebPageRoutedQuery<HomePage>(Page);
public class HomePageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<HomePageQuery, HomePage>(tools)
{
    public override async Task<HomePage> Handle(HomePageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page);

        var r = await Executor.GetMappedWebPageResult<HomePage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
