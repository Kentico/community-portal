using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPageQuery(IRoutedWebPage Page) : WebPageRoutedQuery<BlogPostPage>(Page);
public class BlogPostPageQueryHandler : WebPageQueryHandler<BlogPostPageQuery, BlogPostPage>
{
    public BlogPostPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<BlogPostPage> Handle(BlogPostPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForWebPage(WebsiteChannelContext, BlogPostPage.CONTENT_TYPE_NAME, request.Page, c => c.WithLinkedItems(2));

        var r = await Executor.GetWebPageResult(b, WebPageMapper.Map<BlogPostPage>, DefaultQueryOptions, cancellationToken);

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
                                author.AuthorContentPhotoMediaFileImage,
                                (image, builder) => builder.Media(image))));
}
