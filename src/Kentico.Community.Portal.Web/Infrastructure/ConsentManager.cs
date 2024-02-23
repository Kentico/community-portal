using CMS.ContactManagement;
using CMS.DataProtection;
using CMS.Helpers;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class ConsentManager(
    IConsentAgreementService consentAgreementService,
    IConsentInfoProvider consentProvider,
    IProgressiveCache cache)
{
    public const string MARKETING_CONSENT = "KenticoCommunityPortalTracking";

    private readonly IConsentAgreementService consentAgreementService = consentAgreementService;
    private readonly IConsentInfoProvider consentProvider = consentProvider;
    private readonly IProgressiveCache cache = cache;

    public async Task<ConsentInfo?> GetConsent(string consentCodeName) =>
        await cache.LoadAsync(cs => consentProvider.GetAsync(consentCodeName), new CacheSettings(5, nameof(ConsentInfo), consentCodeName));

    public async Task AgreeToMarketingConsent()
    {
        var currentContact = ContactManagementContext.CurrentContact;

        var consent = await GetConsent(MARKETING_CONSENT);

        if (currentContact is not null && consent is not null)
        {
            _ = consentAgreementService.Agree(currentContact, consent);
        }

        return;
    }
}
