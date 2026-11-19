using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using Kentico.Community.Portal.Core.DataProtection;

namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Cached consent lookup by code name (see <see cref="ConsentCodeNames"/>).
/// </summary>
public class ConsentRetriever(IInfoProvider<ConsentInfo> consentProvider, IProgressiveCache cache)
{
    private readonly IInfoProvider<ConsentInfo> consentProvider = consentProvider;
    private readonly IProgressiveCache cache = cache;

    public async Task<ConsentInfo?> GetConsent(string consentCodeName) =>
        await cache.LoadAsync(cs => consentProvider.GetAsync(consentCodeName), new CacheSettings(5, nameof(ConsentInfo), consentCodeName));
}
