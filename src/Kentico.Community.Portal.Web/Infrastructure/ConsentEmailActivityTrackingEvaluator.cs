using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.EmailLibrary;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.OnlineMarketing.Web.Mvc;

namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Email open/click activities follow the <see cref="ConsentCodeNames.WEBSITE_TRACKING"/> consent,
/// the same one that gates website tracking. Without it, emails only feed anonymous statistics.
/// </summary>
public class ConsentEmailActivityTrackingEvaluator(IConsentAgreementService consentAgreementService, IInfoProvider<ConsentInfo> consentInfoProvider)
    : IEmailActivityTrackingEvaluator
{
    private readonly IConsentAgreementService consentAgreementService = consentAgreementService;
    private readonly IInfoProvider<ConsentInfo> consentInfoProvider = consentInfoProvider;

    public async Task<bool> IsTrackingAllowed(ContactInfo contact, EmailConfigurationInfo emailConfiguration)
    {
        var consent = await consentInfoProvider.GetAsync(ConsentCodeNames.WEBSITE_TRACKING);

        return consent is not null && consentAgreementService.IsAgreed(contact, consent);
    }
}
