using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPageQuery(IRoutedWebPage Page) : WebPageRoutedQuery<BlogPostPageQueryResponse>(Page);
public record BlogPostPageQueryResponse(BlogPostPage Page);
public class BlogPostPageQueryHandler : WebPageQueryHandler<BlogPostPageQuery, BlogPostPageQueryResponse>
{
    public BlogPostPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<BlogPostPageQueryResponse> Handle(BlogPostPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(WebsiteChannelContext, BlogPostPage.CONTENT_TYPE_NAME, request.Page, c => c.WithLinkedItems(1));

        var r = await Executor.GetWebPageResult(b, Mapper.Map<BlogPostPage>, DefaultQueryOptions, cancellationToken);

        return new(r.First());
    }
}
