namespace Kentico.Community.Portal.Core.Content;

public partial class BlogPostContent : IContentFieldsSource
{
    public static class ContentTypes
    {
        public const string MARKDOWN = "markdown";
        public const string RICH_TEXT = "richText";
    }

    public bool IsContentTypeMarkdown() => string.Equals(BlogPostPageContentSourceType, ContentTypes.MARKDOWN, StringComparison.OrdinalIgnoreCase);
}
