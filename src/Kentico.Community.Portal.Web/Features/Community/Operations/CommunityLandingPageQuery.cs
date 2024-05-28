using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Community;

public record CommunityLandingPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<CommunityLandingPage>(Page);
public class CommunityLandingPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<CommunityLandingPageQuery, CommunityLandingPage>(tools)
{
    public override async Task<CommunityLandingPage> Handle(CommunityLandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page);

        var r = await Executor.GetMappedWebPageResult<CommunityLandingPage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
