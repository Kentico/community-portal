namespace Kentico.Community.Portal.Core.Content;

public partial class BlogPostPage
{
    public static class ContentTypes
    {
        public const string MARKDOWN = "markdown";
        public const string RICH_TEXT = "richText";
    }

    public bool IsContentTypeMarkdown() => string.Equals(BlogPostPageContentType, ContentTypes.MARKDOWN, StringComparison.OrdinalIgnoreCase);
}
