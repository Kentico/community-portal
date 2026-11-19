using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using Microsoft.Extensions.Logging;

namespace Kentico.Community.Portal.Core.DataProtection;

/// <summary>
/// Records and revokes <see cref="ConsentCodeNames.EMAIL_MARKETING"/> agreements. Idempotent: agrees
/// only when not agreed, revokes only when agreed, so the inline calls, the confirmation event handler
/// and the reconciliation task can all run for the same subscription. Read-only mode: no-op.
/// </summary>
public class EmailMarketingConsentManager(
    IInfoProvider<ConsentInfo> consentProvider,
    IConsentAgreementService consentAgreementService,
    IReadOnlyModeProvider readOnlyProvider,
    ILogger<EmailMarketingConsentManager> logger)
{
    private readonly IInfoProvider<ConsentInfo> consentProvider = consentProvider;
    private readonly IConsentAgreementService consentAgreementService = consentAgreementService;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;
    private readonly ILogger<EmailMarketingConsentManager> logger = logger;

    /// <returns>True when a new agreement was recorded.</returns>
    public async Task<bool> Agree(ContactInfo contact)
    {
        if (readOnlyProvider.IsReadOnly || await GetConsent() is not ConsentInfo consent)
        {
            return false;
        }

        if (consentAgreementService.IsAgreed(contact, consent))
        {
            return false;
        }

        _ = consentAgreementService.Agree(contact, consent);

        return true;
    }

    /// <returns>True when an agreement was revoked.</returns>
    public async Task<bool> Revoke(ContactInfo contact)
    {
        if (readOnlyProvider.IsReadOnly || await GetConsent() is not ConsentInfo consent)
        {
            return false;
        }

        if (!consentAgreementService.IsAgreed(contact, consent))
        {
            return false;
        }

        consentAgreementService.Revoke(contact, consent);

        return true;
    }

    public async Task<ConsentInfo?> GetConsent()
    {
        var consent = await consentProvider.GetAsync(ConsentCodeNames.EMAIL_MARKETING);
        if (consent is null)
        {
            logger.LogError(new EventId(0, "CONSENT_MISSING"), "Consent {ConsentName} does not exist. Newsletter agreements can't be recorded.", ConsentCodeNames.EMAIL_MARKETING);
        }

        return consent;
    }
}
