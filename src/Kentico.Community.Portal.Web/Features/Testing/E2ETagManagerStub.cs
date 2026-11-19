using CMS.Base;
using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.TagManager.Admin;
using Kentico.Xperience.TagManager.Rendering;

namespace Kentico.Community.Portal.Web.Features.Testing;

/// <summary>
/// Creates or removes a Tag Manager snippet that stands in for Google Tag Manager in E2E runs.
/// Local and CI databases have no GTM snippet (prod's lives only in the prod DB), so without this
/// there is nothing to observe the portal's consent signals with.
/// </summary>
/// <remarks>
/// <para>
/// The stub records every <c>dataLayer</c> entry - including entries pushed before it loaded, the
/// way GTM processes them - into <c>window.__gtmStub</c>. <c>gtag()</c> calls push an
/// <c>arguments</c> object, which is recorded as an array, so <c>gtag('consent','default',{...})</c>
/// shows up as <c>['consent','default',{...}]</c>. Entries are JSON-cloned when recorded.
/// <c>window.__gtmStubLoadedWith</c> is the number of entries that existed when the stub ran, so
/// specs can tell what was pushed before a HeadTop snippet.
/// </para>
/// <para>
/// The snippet must never reach the CI repository. Writes run with CI serialization off, but a
/// manual CI store while the snippet exists would still pick it up, so specs must disable it again
/// when they finish.
/// </para>
/// </remarks>
public class E2ETagManagerStub(
    IInfoProvider<ChannelCodeSnippetItemInfo> snippetProvider,
    IInfoProvider<ChannelInfo> channelProvider)
{
    public const string SNIPPET_NAME = "E2EGtmStub";

    /// <summary>
    /// The Tag Manager package's custom HTML snippet type (<c>CustomSnippetFactory</c>, internal).
    /// </summary>
    private const string CUSTOM_SNIPPET_TYPE = "CustomTag";

    private const string STUB_CODE = """
        <script data-e2e-gtm-stub>
        (function () {
            var stub = window.__gtmStub = window.__gtmStub || [];
            var dataLayer = window.dataLayer = window.dataLayer || [];
            function record(entry) {
                var value = Object.prototype.toString.call(entry) === '[object Arguments]'
                    ? Array.prototype.slice.call(entry)
                    : entry;
                stub.push(JSON.parse(JSON.stringify(value)));
            }
            for (var i = 0; i < dataLayer.length; i++) { record(dataLayer[i]); }
            window.__gtmStubLoadedWith = dataLayer.length;
            var push = dataLayer.push;
            dataLayer.push = function () {
                for (var j = 0; j < arguments.length; j++) { record(arguments[j]); }
                return push.apply(dataLayer, arguments);
            };
        })();
        </script>
        """;

    private readonly IInfoProvider<ChannelCodeSnippetItemInfo> snippetProvider = snippetProvider;
    private readonly IInfoProvider<ChannelInfo> channelProvider = channelProvider;

    public async Task<Result<E2ETagManagerSnippetResponse>> SetEnabled(bool enabled, CancellationToken cancellationToken)
    {
        var existing = await GetSnippet(cancellationToken);

        using var context = new CMSActionContext
        {
            ContinuousIntegrationAllowObjectSerialization = false,
        };

        if (!enabled)
        {
            if (existing is not null)
            {
                snippetProvider.Delete(existing);
            }

            return new E2ETagManagerSnippetResponse(false);
        }

        var channel = (await channelProvider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelName), SystemChannels.WebsiteChannel.CodeName)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();

        if (channel is null)
        {
            return Result.Failure<E2ETagManagerSnippetResponse>($"Website channel '{SystemChannels.WebsiteChannel.CodeName}' was not found.");
        }

        var snippet = existing ?? new ChannelCodeSnippetItemInfo
        {
            ChannelCodeSnippetItemGuid = Guid.NewGuid(),
            ChannelCodeSnippetItemName = SNIPPET_NAME,
        };

        snippet.ChannelCodeSnippetItemChannelId = channel.ChannelID;
        snippet.ChannelCodeSnippetItemType = CUSTOM_SNIPPET_TYPE;
        snippet.ChannelCodeSnippetItemCode = STUB_CODE;
        snippet.ChannelCodeSnippetItemLocation = nameof(CodeSnippetLocations.HeadTop);
        snippet.ChannelCodeSnippetAdministrationDisplayMode = nameof(CodeSnippetAdministrationDisplayMode.None);
        snippet.ChannelCodeSnippetItemEnable = true;
        // No consent agreement: matches prod's GTM snippet. The portal's Analytics choice gates it
        // (ConsentGatedCodeSnippetsService), then Consent Mode gates what it does
        snippet.SetValue(nameof(ChannelCodeSnippetItemInfo.ChannelCodeSnippetItemConsentId), null);

        snippetProvider.Set(snippet);

        return new E2ETagManagerSnippetResponse(true);
    }

    private async Task<ChannelCodeSnippetItemInfo?> GetSnippet(CancellationToken cancellationToken) =>
        (await snippetProvider.Get()
            .WhereEquals(nameof(ChannelCodeSnippetItemInfo.ChannelCodeSnippetItemName), SNIPPET_NAME)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();
}

public record E2ETagManagerSnippetRequest(bool Enabled);

public record E2ETagManagerSnippetResponse(bool Enabled);
