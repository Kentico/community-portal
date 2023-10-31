using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.LandingPages;

public record LandingPageQuery(IRoutedWebPage Page) : WebPageRoutedQuery<LandingPageQueryResponse>(Page);
public record LandingPageQueryResponse(LandingPage Page);
public class LandingPageQueryHandler : WebPageQueryHandler<LandingPageQuery, LandingPageQueryResponse>
{
    public LandingPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<LandingPageQueryResponse> Handle(LandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(WebsiteChannelContext, LandingPage.CONTENT_TYPE_NAME, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<LandingPage>, DefaultQueryOptions, cancellationToken);

        return new(r.First());
    }
}
