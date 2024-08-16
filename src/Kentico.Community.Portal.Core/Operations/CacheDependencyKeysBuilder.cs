using CMS.DataEngine;
using CMS.MediaLibrary;
using CMS.Websites;

namespace Kentico.Community.Portal.Core.Operations;

/// <summary>
/// Generates and stores cache dependency key strings according to https://docs.kentico.com/x/XA_RBg
/// If any provided values are null, whitespace, empty, default, or invalid (negative int identifiers), no cache key will be generated for that value
/// </summary>
public interface ICacheDependencyKeysBuilder
{
    ICacheDependencyKeysBuilder AllContentItems();

    /// <summary>
    /// contentitem|byid|&lt;content item ID&gt;
    /// </summary>
    /// <param name="contentItemID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(int contentItemID);
    /// <summary>
    /// contentitem|byid|&lt;content item ID&gt;
    /// </summary>
    /// <param name="contentItemID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(IContentItemFieldsSource? content);
    /// <summary>
    /// contentitem|byid|&lt;content item ID&gt;
    /// </summary>
    /// <param name="contentItemID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(Maybe<int> contentItemID);
    /// <summary>
    /// contentitem|byid|&lt;content item ID&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="contentItemID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(int contentItemID, string languageName);
    /// <summary>
    /// contentitem|byid|&lt;content item ID&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="contentItemID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(Maybe<int> contentItemID, string languageName);

    /// <summary>
    /// contentitem|byname|&lt;content item name&gt;
    /// </summary>
    /// <param name="contentItemCodeName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(string contentItemCodeName);
    /// <summary>
    /// contentitem|byname|&lt;content item name&gt;
    /// </summary>
    /// <param name="contentItemCodeName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(Maybe<string> contentItemCodeName);
    /// <summary>
    /// contentitem|byname|&lt;content item name&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="contentItemName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(string contentItemName, string languageName);
    /// <summary>
    /// contentitem|byname|&lt;content item name&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="contentItemName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(Maybe<string> contentItemName, string languageName);

    /// <summary>
    /// contentitem|byguid|&lt;content item guid&gt;
    /// </summary>
    /// <param name="contentItemGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(Guid contentItemGUID);
    /// <summary>
    /// contentitem|byguid|&lt;content item guid&gt;
    /// </summary>
    /// <param name="contentItemGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(Maybe<Guid> contentItemGUID);
    /// <summary>
    /// contentitem|byguid|&lt;content item guid&gt|&lt;language name&gt;
    /// </summary>
    /// <param name="contentItemGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(Guid contentItemGUID, string languageName);
    /// <summary>
    /// contentitem|byguid|&lt;content item guid&gt|&lt;language name&gt;
    /// </summary>
    /// <param name="contentItemGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder ContentItem(Maybe<Guid> contentItemGUID, string languageName);

    /// <summary>
    /// contentitem|bycontenttype|&lt;page type code name&gt;
    /// </summary>
    /// <param name="contentTypeName"></param>
    /// <remarks>This generates a cache dependencey key for Pages of a given Type</remarks>
    /// <returns></returns>
    ICacheDependencyKeysBuilder AllContentItems(string contentTypeName);
    /// <summary>
    /// contentitem|bycontenttype|&lt;page type code name&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="contentTypeName"></param>
    /// <remarks>This generates a cache dependencey key for Pages of a given Type</remarks>
    /// <returns></returns>
    ICacheDependencyKeysBuilder AllContentItems(string contentTypeName, string languageName);

    /// <summary>
    /// webpageitem|all
    /// </summary>
    /// <param name="contentTypeName"></param>
    /// <remarks>Generates a cache dependency key for all web pages across all channels</remarks>
    /// <returns></returns>
    ICacheDependencyKeysBuilder AllWebPages();
    /// <summary>
    /// webpageitem|bychannel|&lt;channel name&gt;|all
    /// </summary>
    /// <param name="channelName"></param>
    /// <remarks>Generates a cache dependency key for all web pages in the given channel</remarks>
    /// <returns></returns>
    ICacheDependencyKeysBuilder AllWebPages(string channelName);

    /// <summary>
    /// webpageitem|byid|&lt;web page id&gt;
    /// </summary>
    /// <param name="webPageID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(int webPageID);
    /// <summary>
    /// webpageitem|byid|&lt;web page id&gt;
    /// </summary>
    /// <param name="webPageID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(IWebPageFieldsSource page);
    /// <summary>
    /// webpageitem|byid|&lt;web page id&gt;
    /// </summary>
    /// <param name="webPageID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(Maybe<int> webPageID);
    /// <summary>
    /// webpageitem|byid|&lt;web page id&gt|&lt;language name&gt;
    /// </summary>
    /// <param name="webPageID"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(int webPageID, string languageName);
    /// <summary>
    /// webpageitem|byid|&lt;web page id&gt|&lt;language name&gt;
    /// </summary>
    /// <param name="webPageID"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(Maybe<int> webPageID, string languageName);

    /// <summary>
    /// webpageItem|byname|&lt;web page code name&gt;
    /// </summary>
    /// <param name="webPageCodeName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(string webPageCodeName);
    /// <summary>
    /// webpageItem|byname|&lt;web page code name&gt;
    /// </summary>
    /// <param name="webPageCodeName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(Maybe<string> webPageCodeName);
    /// <summary>
    /// webpageItem|byname|&lt;web page code name&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="webPageCodeName"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(string webPageCodeName, string languageName);
    /// <summary>
    /// webpageItem|byname|&lt;web page code name&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="webPageName"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(Maybe<string> webPageName, string languageName);

    /// <summary>
    /// webpageItem|byguid|&lt;web page guid&gt;
    /// </summary>
    /// <param name="webPageGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(Guid webPageGUID);
    /// <summary>
    /// webpageItem|byguid|&lt;web page guid&gt;
    /// </summary>
    /// <param name="webPageGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(Maybe<Guid> webPageGUID);
    /// <summary>
    /// webpageItem|byguid|&lt;web page guid&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="webPageGUID"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(Guid webPageGUID, string languageName);
    /// <summary>
    /// webpageItem|byguid|&lt;web page guid&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="webPageGUID"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPage(Maybe<Guid> webPageGUID, string languageName);

    /// <summary>
    /// webpageItem|byid|&lt;web page id&gt;
    /// </summary>
    /// <param name="pages"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPages(IEnumerable<IWebPageFieldsSource> pages);
    /// <summary>
    /// webpageItem|byid|&lt;web page id&gt;
    /// </summary>
    /// <param name="webPageID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPages(IEnumerable<int> webPageID);
    /// <summary>
    /// webpageItem|byid|&lt;web page id&gt;
    /// </summary>
    /// <param name="webPageID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPages(IEnumerable<Maybe<int>> webPageID);

    /// <summary>
    /// webpageItem|bychannel|&lt;channel name&gt;|bycontenttype|&lt;content type name&gt;
    /// </summary>
    /// <param name="contentTypeName"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPageType(string contentTypeName, string channelName);
    /// <summary>
    /// webpageItem|bychannel|&lt;channel name&gt;|bycontenttype|&lt;content type name&gt;|&lt;language name&gt;
    /// </summary>
    /// <param name="contentTypeName"></param>
    /// <param name="channelName"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPageType(string contentTypeName, string channelName, string languageName);

    /// <summary>
    /// webpageitem|bychannel|&lt;channel name&gt;|bypath|&lt;path&gt;
    /// </summary>
    /// <param name="path"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPageByPath(string path, string channelName);
    ICacheDependencyKeysBuilder WebPageByPath(Maybe<string> path, string channelName);
    /// <summary>
    /// webpageitem|bychannel|&lt;channel name&gt;|bypath|&lt;path&gt|&lt;language name&gt;
    /// </summary>
    /// <param name="path"></param>
    /// <param name="channelName"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPageByPath(string path, string channelName, string languageName);
    ICacheDependencyKeysBuilder WebPageByPath(Maybe<string> path, string channelName, string languageName);

    /// <summary>
    /// webpageitem|bychannel|&lt;channel name&gt;|childrenofpath|&lt;path&gt;
    /// </summary>
    /// <param name="path"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPageChildrenByPath(string path, string channelName);
    ICacheDependencyKeysBuilder WebPageChildrenByPath(Maybe<string> path, string channelName);
    /// <summary>
    /// webpageitem|bychannel|&lt;channel name&gt;|childrenofpath|&lt;path&gt|&lt;language name&gt;
    /// </summary>
    /// <param name="path"></param>
    /// <param name="channelName"></param>
    /// <param name="languageName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder WebPageChildrenByPath(string path, string channelName, string languageName);
    ICacheDependencyKeysBuilder WebPageChildrenByPath(Maybe<string> path, string channelName, string languageName);

    /// <summary>
    /// &lt;object type&gt;|all
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder AllObjects(string typeName);
    /// <summary>
    /// &lt;object type&gt;|byid|&lt;id&gt;
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Object(string objectType, int objectID);
    /// <summary>
    /// &lt;object type&gt;|byid|&lt;id&gt;
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Object(string objectType, Maybe<int> objectID);
    /// <summary>
    /// &lt;object type&gt;|byname|&lt;object code name&gt;
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectCodeName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Object(string objectType, string objectCodeName);
    /// <summary>
    /// &lt;object type&gt;|byname|&lt;object code name&gt;
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectCodeName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Object(string objectType, Maybe<string> objectCodeName);
    /// <summary>
    /// &lt;object type&gt;|byguid|&lt;id&gt;
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Object(string objectType, Guid objectGUID);
    /// <summary>
    /// &lt;object type&gt;|byguid|&lt;id&gt;
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Object(string objectType, Maybe<Guid> objectGUID);
    /// <summary>
    /// &lt;object type&gt;|byid|&lt;id&gt;
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Objects(IEnumerable<BaseInfo> objects);

    /// <summary>
    /// &lt;cms.settingskey|key name&gt;
    /// </summary>
    /// <param name="keyName"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder SettingsKey(string keyName);

    /// <summary>
    /// mediafile|&lt;guid&gt;
    /// </summary>
    /// <param name="mediaFileGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Media(Guid mediaFileGUID);
    /// <summary>
    /// mediafile|&lt;guid&gt;
    /// </summary>
    /// <param name="mediaFileGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Media(AssetRelatedItem asset);
    /// <summary>
    /// mediafile|&lt;guid&gt;
    /// </summary>
    /// <param name="mediaFileGUID"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder Media(Maybe<Guid> mediaFileGUID);

    /// <summary>
    /// Executes the <paramref name="action"/> on each item in the collectio <paramref name="items"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <example>
    /// var items = new[] { 1, 3, 4 };
    /// 
    /// builder.Collection(items, (i, b) => b.Node(i));
    /// </example>
    ICacheDependencyKeysBuilder Collection<T>(IEnumerable<T>? items, Action<T, ICacheDependencyKeysBuilder> action);
    /// <summary>
    /// Executes the <paramref name="action"/> on each item in the collectio <paramref name="items"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <example>
    /// var items = new[] { 1, 3, 4 };
    /// 
    /// builder.Collection(items, (i, b) => b.Node(i));
    /// </example>
    ICacheDependencyKeysBuilder Collection<T>(IEnumerable<Maybe<T>> items, Action<T, ICacheDependencyKeysBuilder> action);
    /// <summary>
    /// Executes the <paramref name="action"/> on each item in the collectio <paramref name="items"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <example>
    /// var items = new[] { 1, 3, 4 };
    /// 
    /// builder.Collection(items, (i) => builder.Node(i));
    /// </example>
    ICacheDependencyKeysBuilder Collection<T>(IEnumerable<T> items, Action<T> action);
    /// <summary>
    /// Executes the <paramref name="action"/> on each item in the collectio <paramref name="items"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <example>
    /// var items = new[] { 1, 3, 4 };
    /// 
    /// builder.Collection(items, (i) => builder.Node(i));
    /// </example>
    ICacheDependencyKeysBuilder Collection<T>(IEnumerable<Maybe<T>> items, Action<T> action);
    /// <summary>
    /// Can be used to add any custom key to the builder
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder CustomKey(string key);
    /// <summary>
    /// Can be used to add any custom keys to the builder
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    ICacheDependencyKeysBuilder CustomKeys(IEnumerable<string> keys);
}

public class CacheDependencyKeysBuilder : ICacheDependencyKeysBuilder
{
    private readonly HashSet<string> dependencyKeys = new(StringComparer.InvariantCultureIgnoreCase);

    public ISet<string> GetKeys() => dependencyKeys;

    public ICacheDependencyKeysBuilder AllContentItems()
    {
        _ = dependencyKeys.Add("contentitem|all");

        return this;
    }

    public ICacheDependencyKeysBuilder ContentItem(Maybe<int> contentItemID) =>
        contentItemID.TryGetValue(out int id)
            ? ContentItem(id)
            : this;
    public ICacheDependencyKeysBuilder ContentItem(int contentItemID)
    {
        if (contentItemID <= 0)
        {
            return this;
        }

        _ = dependencyKeys.Add($"contentitem|byid|{contentItemID}");

        return this;
    }
    public ICacheDependencyKeysBuilder ContentItem(IContentItemFieldsSource? content) =>
        content is null
            ? this
            : ContentItem(content.SystemFields.ContentItemID);
    public ICacheDependencyKeysBuilder ContentItem(Maybe<int> contentItemID, string languageName) =>
        contentItemID.TryGetValue(out int id)
            ? ContentItem(id, languageName)
            : this;
    public ICacheDependencyKeysBuilder ContentItem(int contentItemID, string languageName)
    {
        if (contentItemID <= 0)
        {
            return this;
        }

        _ = dependencyKeys.Add($"contentitem|byid|{contentItemID}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder ContentItem(Maybe<string> contentItemCodeName) =>
        contentItemCodeName.TryGetValue(out string? name)
            ? ContentItem(name)
            : this;
    public ICacheDependencyKeysBuilder ContentItem(string? contentItemCodeName)
    {
        if (string.IsNullOrWhiteSpace(contentItemCodeName))
        {
            return this;
        }

        _ = dependencyKeys.Add($"contentitem|byname|{contentItemCodeName}");

        return this;
    }
    public ICacheDependencyKeysBuilder ContentItem(Maybe<string> contentItemCodeName, string languageName) =>
        contentItemCodeName.TryGetValue(out string? name)
            ? ContentItem(name, languageName)
            : this;
    public ICacheDependencyKeysBuilder ContentItem(string? contentItemCodeName, string languageName)
    {
        if (string.IsNullOrWhiteSpace(contentItemCodeName))
        {
            return this;
        }

        _ = dependencyKeys.Add($"contentitem|byname|{contentItemCodeName}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder ContentItem(Maybe<Guid> nodeGUID) =>
        nodeGUID.TryGetValue(out var guid)
            ? ContentItem(guid)
            : this;
    public ICacheDependencyKeysBuilder ContentItem(Guid nodeGUID)
    {
        if (nodeGUID == default)
        {
            return this;
        }

        _ = dependencyKeys.Add($"contentitem|byguid|{nodeGUID}");

        return this;
    }
    public ICacheDependencyKeysBuilder ContentItem(Maybe<Guid> nodeGUID, string languageName) =>
        nodeGUID.TryGetValue(out var guid)
            ? ContentItem(guid, languageName)
            : this;
    public ICacheDependencyKeysBuilder ContentItem(Guid nodeGUID, string languageName)
    {
        if (nodeGUID == default)
        {
            return this;
        }

        _ = dependencyKeys.Add($"contentitem|byguid|{nodeGUID}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder AllContentItems(string contentTypeName)
    {
        _ = dependencyKeys.Add($"contentitem|bycontenttype|{contentTypeName}");

        return this;
    }
    public ICacheDependencyKeysBuilder AllContentItems(string contentTypeName, string languageName)
    {
        _ = dependencyKeys.Add($"contentitem|bycontenttype|{contentTypeName}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder AllWebPages()
    {
        _ = dependencyKeys.Add("webpageitem|all");

        return this;
    }
    public ICacheDependencyKeysBuilder AllWebPages(string channelName)
    {
        _ = dependencyKeys.Add($"webpageitem|bychannel|{channelName}|all");

        return this;
    }

    public ICacheDependencyKeysBuilder WebPage(Maybe<int> webPageID) =>
        webPageID.TryGetValue(out int id)
            ? WebPage(id)
            : this;
    public ICacheDependencyKeysBuilder WebPage(int webPageID)
    {
        if (webPageID <= 0)
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|byid|{webPageID}");

        return this;
    }
    public ICacheDependencyKeysBuilder WebPage(Maybe<int> webPageID, string languageName) =>
        webPageID.TryGetValue(out int id)
            ? WebPage(id, languageName)
            : this;
    public ICacheDependencyKeysBuilder WebPage(int webPageID, string languageName)
    {
        if (webPageID <= 0)
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|byid|{webPageID}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder WebPage(Maybe<string> webPageName) =>
        webPageName.TryGetValue(out string? name)
            ? WebPage(name)
            : this;
    public ICacheDependencyKeysBuilder WebPage(string? webPageName)
    {
        if (string.IsNullOrWhiteSpace(webPageName))
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|byname|{webPageName}");

        return this;
    }
    public ICacheDependencyKeysBuilder WebPage(Maybe<string> webPageName, string languageName) =>
        webPageName.TryGetValue(out string? name)
            ? WebPage(name, languageName)
            : this;
    public ICacheDependencyKeysBuilder WebPage(string? webPageName, string languageName)
    {
        if (string.IsNullOrWhiteSpace(webPageName))
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|byname|{webPageName}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder WebPage(Maybe<Guid> webPageGUID) =>
        webPageGUID.TryGetValue(out var guid)
            ? WebPage(guid)
            : this;
    public ICacheDependencyKeysBuilder WebPage(Guid webPageGUID)
    {
        if (webPageGUID == default)
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|byguid|{webPageGUID}");

        return this;
    }
    public ICacheDependencyKeysBuilder WebPage(Maybe<Guid> webPageGUID, string languageName) =>
        webPageGUID.TryGetValue(out var guid)
            ? WebPage(guid, languageName)
            : this;
    public ICacheDependencyKeysBuilder WebPage(IWebPageFieldsSource page) =>
        WebPage(page.SystemFields.WebPageItemID);
    public ICacheDependencyKeysBuilder WebPage(Guid webPageGUID, string languageName)
    {
        if (webPageGUID == default)
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|byguid|{webPageGUID}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder WebPages(IEnumerable<IWebPageFieldsSource> pages)
    {
        dependencyKeys.UnionWith(pages.Select(p => $"webpageitem|byid|{p.SystemFields.WebPageItemID}"));

        return this;
    }
    public ICacheDependencyKeysBuilder WebPages(IEnumerable<Maybe<int>> webPageIDs) =>
        WebPages(webPageIDs.Choose());
    public ICacheDependencyKeysBuilder WebPages(IEnumerable<int> webPageIDs)
    {
        dependencyKeys.UnionWith(webPageIDs.Select(id => $"webpageitem|byid|{id}"));

        return this;
    }
    public ICacheDependencyKeysBuilder WebPageType(string contentTypeName, string channelName)
    {
        _ = dependencyKeys.Add($"webpageitem|bychannel|{channelName}|bycontenttype|{contentTypeName}");

        return this;
    }
    public ICacheDependencyKeysBuilder WebPageType(string contentTypeName, string channelName, string languageName)
    {
        _ = dependencyKeys.Add($"webpageitem|bychannel|{channelName}|bycontenttype|{contentTypeName}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder WebPageByPath(Maybe<string> path, string channelName) =>
        path.TryGetValue(out string? p)
            ? WebPageByPath(p, channelName)
            : this;
    public ICacheDependencyKeysBuilder WebPageByPath(string? path, string channelName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|bychannel|{channelName}|bypath|{path}");

        return this;
    }
    public ICacheDependencyKeysBuilder WebPageByPath(Maybe<string> path, string channelName, string languageName) =>
        path.TryGetValue(out string? p)
            ? WebPageByPath(p, channelName, languageName)
            : this;
    public ICacheDependencyKeysBuilder WebPageByPath(string? path, string channelName, string languageName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|bychannel|{channelName}|bypath|{path}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder WebPageChildrenByPath(Maybe<string> path, string channelName) =>
        path.TryGetValue(out string? p)
            ? WebPageChildrenByPath(p, channelName)
            : this;
    public ICacheDependencyKeysBuilder WebPageChildrenByPath(string? path, string channelName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|bychannel|{channelName}|childrenofpath|{path}");

        return this;
    }
    public ICacheDependencyKeysBuilder WebPageChildrenByPath(Maybe<string> path, string channelName, string languageName) =>
        path.TryGetValue(out string? p)
            ? WebPageChildrenByPath(p, channelName, languageName)
            : this;
    public ICacheDependencyKeysBuilder WebPageChildrenByPath(string? path, string channelName, string languageName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return this;
        }

        _ = dependencyKeys.Add($"webpageitem|bychannel|{channelName}|childrenofpath|{path}|{languageName}");

        return this;
    }

    public ICacheDependencyKeysBuilder AllObjects(string typeName)
    {
        _ = dependencyKeys.Add($"{typeName}|all");

        return this;
    }

    public ICacheDependencyKeysBuilder SettingsKey(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
        {
            return this;
        }

        _ = dependencyKeys.Add($"cms.settingskey|{keyName}");

        return this;
    }


    public ICacheDependencyKeysBuilder Object(string objectType, Maybe<int> objectID) =>
        objectID.TryGetValue(out int id)
            ? Object(objectType, id)
            : this;
    public ICacheDependencyKeysBuilder Object(string objectType, int objectID)
    {
        if (string.IsNullOrWhiteSpace(objectType) || objectID <= 0)
        {
            return this;
        }

        _ = dependencyKeys.Add($"{objectType}|byid|{objectID}");

        return this;
    }

    public ICacheDependencyKeysBuilder Object(string objectType, Maybe<string> objectCodeName) =>
        objectCodeName.TryGetValue(out string? name)
            ? Object(objectType, name)
            : this;
    public ICacheDependencyKeysBuilder Object(string objectType, string? objectCodeName)
    {
        if (string.IsNullOrWhiteSpace(objectType) || string.IsNullOrWhiteSpace(objectCodeName))
        {
            return this;
        }

        _ = dependencyKeys.Add($"{objectType}|byname|{objectCodeName}");

        return this;
    }

    public ICacheDependencyKeysBuilder Object(string objectType, Maybe<Guid> guid) =>
        Object(objectType, guid.GetValueOrDefault());
    public ICacheDependencyKeysBuilder Object(string objectType, Guid guid)
    {
        if (guid == default)
        {
            return this;
        }

        _ = dependencyKeys.Add($"{objectType}|byguid|{guid}");

        return this;
    }

    public ICacheDependencyKeysBuilder Objects(IEnumerable<BaseInfo> objects)
    {
        dependencyKeys.UnionWith(objects.Select(o => $"{o.TypeInfo.ObjectType}|byid|{o.Generalized.ObjectID}"));

        return this;
    }

    public ICacheDependencyKeysBuilder Media(Maybe<Guid> mediaFileGUID) =>
        Media(mediaFileGUID.GetValueOrDefault());
    public ICacheDependencyKeysBuilder Media(AssetRelatedItem asset) =>
        Media(asset.Identifier);
    public ICacheDependencyKeysBuilder Media(Guid mediaFileGUID)
    {
        if (mediaFileGUID == default)
        {
            return this;
        }

        _ = dependencyKeys.Add($"mediafile|{mediaFileGUID}");

        return this;
    }

    public ICacheDependencyKeysBuilder Collection<T>(IEnumerable<Maybe<T>> items, Action<T, ICacheDependencyKeysBuilder> action) =>
        Collection(items.Choose(), action);
    public ICacheDependencyKeysBuilder Collection<T>(IEnumerable<T>? items, Action<T, ICacheDependencyKeysBuilder> action)
    {
        if (items is null)
        {
            return this;
        }

        foreach (var item in items)
        {
            action(item, this);
        }

        return this;
    }

    public ICacheDependencyKeysBuilder Collection<T>(IEnumerable<Maybe<T>> items, Action<T> action) =>
        Collection(items.Choose(), action);
    public ICacheDependencyKeysBuilder Collection<T>(IEnumerable<T> items, Action<T> action)
    {
        foreach (var item in items)
        {
            action(item);
        }

        return this;
    }

    public ICacheDependencyKeysBuilder CustomKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return this;
        }

        _ = dependencyKeys.Add(key);

        return this;
    }
    public ICacheDependencyKeysBuilder CustomKeys(IEnumerable<string> keys)
    {
        dependencyKeys.UnionWith(keys);

        return this;
    }
}
