using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.EmailLibrary;
using Kentico.OnlineMarketing.Web.Mvc;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class ConsentEmailActivityTrackingEvaluator(IConsentAgreementService consentAgreementService, IInfoProvider<ConsentInfo> consentInfoProvider)
    : IEmailActivityTrackingEvaluator
{
    private readonly IConsentAgreementService consentAgreementService = consentAgreementService;
    private readonly IInfoProvider<ConsentInfo> consentInfoProvider = consentInfoProvider;

    public async Task<bool> IsTrackingAllowed(ContactInfo contact, EmailConfigurationInfo emailConfiguration)
    {
        var consent = await consentInfoProvider.GetAsync("KenticoCommunityPortalTracking");

        return consent is not null && consentAgreementService.IsAgreed(contact, consent);
    }
}
