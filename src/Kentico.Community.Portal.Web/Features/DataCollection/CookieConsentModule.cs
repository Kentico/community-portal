using CMS;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Web.Mvc;
using CookieLevel = Kentico.Web.Mvc.CookieLevel;

[assembly: RegisterModule(typeof(CookieConsentModule))]

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Stops tracking in the current request when its visitor's Website tracking agreement is revoked
/// (Xperience docs pattern). Revokes for other visitors (admin, another browser) are caught by
/// <see cref="CookieConsentMiddleware"/> on their next request.
/// </summary>
internal class CookieConsentModule : Module
{
    private IServiceProvider services = null!;

    public CookieConsentModule() : base(nameof(CookieConsentModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        services = parameters.Services;

        DataProtectionEvents.RevokeConsentAgreement.Execute += RevokeConsentAgreement_Execute;
    }

    private void RevokeConsentAgreement_Execute(object? sender, RevokeConsentAgreementEventArgs e)
    {
        if (!string.Equals(e.Consent.ConsentName, ConsentCodeNames.WEBSITE_TRACKING, StringComparison.OrdinalIgnoreCase)
            || services.GetService<IHttpContextAccessor>()?.HttpContext is not { } context)
        {
            return;
        }

        var currentContact = ContactManagementContext.GetCurrentContact(createAnonymous: false);
        if (currentContact is null || currentContact.ContactID != e.Contact.ContactID)
        {
            return;
        }

        context.RequestServices
            .GetRequiredService<ICurrentCookieLevelProvider>()
            .SetCurrentCookieLevel(CookieLevel.Essential.Level);
    }
}
