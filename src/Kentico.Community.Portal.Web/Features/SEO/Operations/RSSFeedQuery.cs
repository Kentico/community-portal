using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.SEO;

public record RSSFeedPageQuery(RoutedWebPage Page, string ChannelName) : WebPageRoutedQuery<RSSFeedPage>(Page), IChannelContentQuery;
public class RSSFeedPageQueryHandler : WebPageQueryHandler<RSSFeedPageQuery, RSSFeedPage>
{
    public RSSFeedPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<RSSFeedPage> Handle(RSSFeedPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.ChannelName, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<RSSFeedPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
