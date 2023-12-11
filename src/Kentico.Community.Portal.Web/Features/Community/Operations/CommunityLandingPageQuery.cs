using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Community;

public record CommunityLandingPageQuery(RoutedWebPage Page, string ChannelName) : WebPageRoutedQuery<CommunityLandingPage>(Page), IChannelContentQuery;
public class CommunityLandingPageQueryHandler : WebPageQueryHandler<CommunityLandingPageQuery, CommunityLandingPage>
{
    public CommunityLandingPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<CommunityLandingPage> Handle(CommunityLandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.ChannelName, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<CommunityLandingPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
