using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.LandingPages;

public record LandingPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<LandingPage>(Page);
public class LandingPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<LandingPageQuery, LandingPage>(tools)
{
    public override async Task<LandingPage> Handle(LandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page);

        var r = await Executor.GetMappedWebPageResult<LandingPage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
