namespace Kentico.Community.Portal.Core.Content;

public partial class BlogPostPage
{
    /// <summary>
    /// Linked items query depth to retrieve a fully hydrated object graph
    /// </summary>
    /// <value></value>
    public static int FullQueryDepth { get; } = 3;

    /// <summary>
    /// Cascading logic across all relevant <see cref="BlogPostPage"/> fields to create a <see cref="WebpageMeta"/>
    /// </summary>
    /// <returns></returns>
    public WebpageMeta GetWebpageMeta()
    {
        string metaTitle = Maybe
            .From(WebPageMetaTitle)
            .MapNullOrWhiteSpaceAsNone()
            .IfNoValue(BlogPostPageBlogPostContent
                .TryFirst()
                .Bind(c => Maybe.From(c.ListableItemTitle).MapNullOrWhiteSpaceAsNone()))
            .GetValueOrDefault("");

        string metaDescription = Maybe
            .From(WebPageMetaDescription)
            .MapNullOrWhiteSpaceAsNone()
            .IfNoValue(BlogPostPageBlogPostContent
                .TryFirst()
                .Bind(c => Maybe.From(c.ListableItemShortDescription).MapNullOrWhiteSpaceAsNone()))
            .GetValueOrDefault("");

        return new WebpageMeta(metaTitle, metaDescription)
        {
            CanonicalURL = WebPageCanonicalURL
        };
    }
}
