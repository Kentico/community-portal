using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Home;

public record HomePageQuery(IRoutedWebPage Page) : WebPageRoutedQuery<HomePage>(Page);
public class HomePageQueryHandler : WebPageQueryHandler<HomePageQuery, HomePage>
{
    public HomePageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<HomePage> Handle(HomePageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(WebsiteChannelContext, HomePage.CONTENT_TYPE_NAME, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<HomePage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
