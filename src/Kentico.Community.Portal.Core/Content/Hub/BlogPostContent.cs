namespace Kentico.Community.Portal.Core.Content;

public partial class BlogPostContent
{
    public static class ContentTypes
    {
        public const string MARKDOWN = "markdown";
        public const string RICH_TEXT = "richText";
    }

    public bool IsContentTypeMarkdown() => string.Equals(BlogPostContentSourceType, ContentTypes.MARKDOWN, StringComparison.OrdinalIgnoreCase);
}
