namespace Kentico.Community.Portal.Core.Content;

public partial class BlogPostPage
{
    /// <summary>
    /// Cascading logic across all relevant <see cref="BlogPostPage"/> fields to create a <see cref="WebpageMeta"/>
    /// </summary>
    /// <returns></returns>
    public WebpageMeta GetWebpageMeta()
    {
        string metaTitle = Maybe
            .From(WebPageMetaTitle)
            .WithNullOrWhiteSpaceAsNone()
            .Match(
                title => title,
                () => BlogPostPageBlogPostContent
                    .TryFirst()
                    .Map(c => Maybe.From(c.ListableItemTitle).WithNullOrWhiteSpaceAsNone().GetValueOrDefault(c.BlogPostContentTitle)))
            .GetValueOrDefault("");

        string metaDescription = Maybe
            .From(WebPageMetaDescription)
            .WithNullOrWhiteSpaceAsNone()
            .Match(
                desc => desc,
                () => BlogPostPageBlogPostContent
                    .TryFirst()
                    .Map(c => Maybe.From(c.ListableItemShortDescription).WithNullOrWhiteSpaceAsNone().GetValueOrDefault(c.BlogPostContentShortDescription)))
            .GetValueOrDefault("");

        return new WebpageMeta(metaTitle, metaDescription)
        {
            CanonicalURL = WebPageCanonicalURL
        };
    }
}
