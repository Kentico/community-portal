using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Community;

public record CommunityLinksPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<CommunityLinksPage>(Page);
public class CommunityLinksPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<CommunityLinksPageQuery, CommunityLinksPage>(tools)
{
    public override async Task<CommunityLinksPage> Handle(CommunityLinksPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page);

        var r = await Executor.GetMappedWebPageResult<CommunityLinksPage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
