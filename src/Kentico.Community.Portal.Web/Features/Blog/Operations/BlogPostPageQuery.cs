using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<BlogPostPage>(Page);
public class BlogPostPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<BlogPostPageQuery, BlogPostPage>(tools)
{
    public override async Task<BlogPostPage> Handle(BlogPostPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page, c => c.WithLinkedItems(3));

        var r = await Executor.GetMappedWebPageResult<BlogPostPage>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostPageQuery query, BlogPostPage result, ICacheDependencyKeysBuilder builder) =>
        base.AddDependencyKeys(query, result, builder)
            .Collection(
                result.BlogPostPageBlogPostContent,
                (content, builder) => builder
                    .ContentItem(content)
                    .Collection(
                        content.BlogPostContentAuthor,
                        (author, builder) => builder
                            .ContentItem(author)
                            .Collection(
                                author.AuthorContentPhoto,
                                (image, builder) => builder.ContentItem(image))));
}
