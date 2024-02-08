using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public record IntegrationsLandingPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<IntegrationsLandingPage>(Page);
public class IntegrationsLandingPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<IntegrationsLandingPageQuery, IntegrationsLandingPage>(tools)
{
    public override async Task<IntegrationsLandingPage> Handle(IntegrationsLandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page.WebsiteChannelName, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<IntegrationsLandingPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
