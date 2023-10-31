using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogLandingPageQuery(IRoutedWebPage Page) : WebPageRoutedQuery<BlogLandingPage>(Page);
public class BlogLandingPageQueryHandler : WebPageQueryHandler<BlogLandingPageQuery, BlogLandingPage>
{
    public BlogLandingPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<BlogLandingPage> Handle(BlogLandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(WebsiteChannelContext, BlogLandingPage.CONTENT_TYPE_NAME, request.Page);

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<BlogLandingPage>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }
}
