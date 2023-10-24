namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Provides methods to format cache dependencies.
/// </summary>
public class CacheDependencyKeyProvider
{
    private const string GENERIC_PAGES_KEY_FORMAT = "node|{0}|/|childnodes";

    /// <summary>
    /// Cache key format for pages.
    /// </summary>
    public const string PAGES_KEY_FORMAT = "nodes|{0}|{1}|all";

    /// <summary>
    /// Cache key format for object.
    /// </summary>
    public const string OBJECTS_KEY_FORMAT = "{0}|all";

    /// <summary>
    /// Gets dependency cache key for given page type.
    /// </summary>
    /// <param name="siteName">Site name.</param>
    /// <param name="className">Class name representing a page type.</param>
    /// <remarks>If class name not provided, dependency key for all pages is returned.</remarks>
    public static string GetDependencyCacheKeyForPageType(string siteName, string className)
    {
        string key = string.IsNullOrEmpty(className)
            ? string.Format(GENERIC_PAGES_KEY_FORMAT, siteName.ToLowerInvariant())
            : string.Format(PAGES_KEY_FORMAT, siteName.ToLowerInvariant(), className.ToLowerInvariant());

        return key;
    }

    /// <summary>
    /// Gets dependency cache key for given object type.
    /// </summary>
    /// <param name="objectType">Object type.</param>
    public static string GetDependencyCacheKeyForObjectType(string objectType)
    {
        if (string.IsNullOrEmpty(objectType))
        {
            throw new NotSupportedException("The object type needs to be provided.");
        }

        return string.Format(OBJECTS_KEY_FORMAT, objectType.ToLowerInvariant());
    }
}
