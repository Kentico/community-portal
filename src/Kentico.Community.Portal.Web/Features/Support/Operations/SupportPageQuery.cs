using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Support;

public record SupportPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<SupportPage>(Page);

public class SupportPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<SupportPageQuery, SupportPage>(tools)
{
    public override async Task<SupportPage> Handle(SupportPageQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page);

        var r = await Executor.GetMappedWebPageResult<SupportPage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
