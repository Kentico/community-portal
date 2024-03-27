using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.ResourceHub;

public record ResourceHubPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<ResourceHubPage>(Page);
public class ResourceHubPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<ResourceHubPageQuery, ResourceHubPage>(tools)
{
    public override async Task<ResourceHubPage> Handle(ResourceHubPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page);

        var r = await Executor.GetMappedResult<ResourceHubPage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
