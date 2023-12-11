using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.LandingPages;

public record LandingPageQuery(RoutedWebPage Page, string ChannelName) : WebPageRoutedQuery<LandingPage>(Page), IChannelContentQuery;
public class LandingPageQueryHandler : WebPageQueryHandler<LandingPageQuery, LandingPage>
{
    public LandingPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<LandingPage> Handle(LandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.ChannelName, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<LandingPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
