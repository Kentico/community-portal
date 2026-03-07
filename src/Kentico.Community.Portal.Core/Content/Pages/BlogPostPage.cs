namespace Kentico.Community.Portal.Core.Content;

public partial class BlogPostPage
{
    public static Guid CONTENT_TYPE_GUID { get; } = new Guid("a0450c6e-a032-40cb-893f-ce121cb22c0e");

    /// <summary>
    /// Linked items query depth to retrieve a fully hydrated object graph
    /// </summary>
    /// <value></value>
    public const int FullQueryDepth = 3;
}
