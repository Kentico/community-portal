using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.ResourceHub;

public record ResourceHubPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<ResourceHubPage>(Page);
public class ResourceHubPageQueryHandler : WebPageQueryHandler<ResourceHubPageQuery, ResourceHubPage>
{
    public ResourceHubPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<ResourceHubPage> Handle(ResourceHubPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(WebsiteChannelContext, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<ResourceHubPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
