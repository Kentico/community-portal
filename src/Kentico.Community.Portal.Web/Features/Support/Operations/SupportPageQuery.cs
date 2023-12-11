using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Support;

public record SupportPageQuery(RoutedWebPage Page, string ChannelName) : WebPageRoutedQuery<SupportPage>(Page), IChannelContentQuery;

public class SupportPageQueryHandler : WebPageQueryHandler<SupportPageQuery, SupportPage>
{
    public SupportPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<SupportPage> Handle(SupportPageQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.ChannelName, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<SupportPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
