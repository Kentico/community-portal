using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public record IntegrationsLandingPageQuery(RoutedWebPage Page, string ChannelName) : WebPageRoutedQuery<IntegrationsLandingPage>(Page), IChannelContentQuery;
public class IntegrationsLandingPageQueryHandler : WebPageQueryHandler<IntegrationsLandingPageQuery, IntegrationsLandingPage>
{
    public IntegrationsLandingPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<IntegrationsLandingPage> Handle(IntegrationsLandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.ChannelName, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<IntegrationsLandingPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
