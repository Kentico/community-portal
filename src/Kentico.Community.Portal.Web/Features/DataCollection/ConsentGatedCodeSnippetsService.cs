using CMS.ContactManagement;
using CMS.DataEngine;
using Kentico.Xperience.TagManager.Admin;
using Kentico.Xperience.TagManager.Rendering;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Decorates Tag Manager's snippet service so no snippet renders until the visitor allows
/// Analytics, except snippets in <see cref="TagManagerConsentOptions.AlwaysAllowedSnippetNames"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tag Manager uses this service for page rendering (<c>CodeSnippetTagHelperComponent</c>) and for
/// <c>/kentico.tagmanager/GetConsentedTags</c>, which <c>window.xperience.tagManager.updateCodeSnippets()</c>
/// calls after a choice (<see cref="ConsentModeScript.ForChoice"/>) to load snippets without a reload.
/// </para>
/// <para>
/// Singleton, like the service it wraps, so the visitor's choice is read per call from the
/// request's <see cref="CookieConsentManager"/>. No request: nothing renders. The inner service
/// caches per contact; this gate runs outside that cache.
/// </para>
/// </remarks>
public class ConsentGatedCodeSnippetsService(
    IChannelCodeSnippetsService inner,
    IHttpContextAccessor contextAccessor,
    IOptionsMonitor<TagManagerConsentOptions> options,
    IInfoProvider<ChannelCodeSnippetItemInfo> snippetProvider) : IChannelCodeSnippetsService
{
    private static readonly ILookup<CodeSnippetLocations, CodeSnippetDto> none =
        Array.Empty<CodeSnippetDto>().ToLookup(s => s.Location);

    private readonly IChannelCodeSnippetsService inner = inner;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly IOptionsMonitor<TagManagerConsentOptions> options = options;
    private readonly IInfoProvider<ChannelCodeSnippetItemInfo> snippetProvider = snippetProvider;

    public async Task<ILookup<CodeSnippetLocations, CodeSnippetDto>> GetConsentedCodeSnippets(ContactInfo? contact, int? contentTypeId = null)
    {
        if (AnalyticsAllowed())
        {
            return await inner.GetConsentedCodeSnippets(contact, contentTypeId);
        }

        var alwaysAllowedNames = options.CurrentValue.AlwaysAllowedSnippetNames;
        if (alwaysAllowedNames.Count == 0)
        {
            return none;
        }

        var snippets = await inner.GetConsentedCodeSnippets(contact, contentTypeId);
        var alwaysAllowedIds = await GetSnippetIds(alwaysAllowedNames);

        return OnlyAllowed(snippets, alwaysAllowedIds);
    }

    /// <summary>The snippets whose ID is in <paramref name="allowedIds"/>.</summary>
    public static ILookup<CodeSnippetLocations, CodeSnippetDto> OnlyAllowed(
        ILookup<CodeSnippetLocations, CodeSnippetDto> snippets,
        IReadOnlySet<int> allowedIds) =>
        snippets
            .SelectMany(group => group)
            .Where(snippet => allowedIds.Contains(snippet.ID))
            .ToLookup(snippet => snippet.Location);

    private bool AnalyticsAllowed() =>
        contextAccessor.HttpContext?.RequestServices.GetService<CookieConsentManager>()?.GetChoice() is { Analytics: true };

    private async Task<HashSet<int>> GetSnippetIds(IEnumerable<string> names) =>
        (await snippetProvider.Get()
            .WhereIn(nameof(ChannelCodeSnippetItemInfo.ChannelCodeSnippetItemName), names.ToArray())
            .Column(nameof(ChannelCodeSnippetItemInfo.ChannelCodeSnippetItemID))
            .GetEnumerableTypedResultAsync())
            .Select(s => s.ChannelCodeSnippetItemID)
            .ToHashSet();
}
