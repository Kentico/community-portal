using CMS;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailMarketing;
using Kentico.Community.Portal.Web.Features.Newsletter;

[assembly: RegisterModule(typeof(EmailMarketingAgreementModule))]

namespace Kentico.Community.Portal.Web.Features.Newsletter;

/// <summary>
/// Hooks newsletter subscription confirmation writes, which include Xperience's own double opt-in
/// and unsubscribe handling, to <see cref="EmailMarketingAgreementHandler"/>. A failure is logged and
/// never breaks the subscription write; the reconciliation task repairs the agreement later.
/// </summary>
internal class EmailMarketingAgreementModule : Module
{
    private IServiceProvider services = null!;

    public EmailMarketingAgreementModule() : base(nameof(EmailMarketingAgreementModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        services = parameters.Services;

        EmailSubscriptionConfirmationInfo.TYPEINFO.Events.Insert.After += Confirmation_SaveAfter;
        EmailSubscriptionConfirmationInfo.TYPEINFO.Events.Update.After += Confirmation_SaveAfter;
        EmailSubscriptionConfirmationInfo.TYPEINFO.Events.Delete.After += Confirmation_DeleteAfter;
    }

    private void Confirmation_SaveAfter(object? sender, ObjectEventArgs e) =>
        Handle(e, (handler, confirmation) => handler.OnSaved(confirmation));

    private void Confirmation_DeleteAfter(object? sender, ObjectEventArgs e) =>
        Handle(e, (handler, confirmation) => handler.OnDeleted(confirmation));

    private void Handle(ObjectEventArgs e, Func<EmailMarketingAgreementHandler, EmailSubscriptionConfirmationInfo, Task> action)
    {
        if (e.Object is not EmailSubscriptionConfirmationInfo confirmation)
        {
            return;
        }

        /*
         * A new scope because modules can only resolve transient and singleton
         * services, and the handler uses scoped ones
         */
        using var scope = services.CreateScope();

        try
        {
            action(scope.ServiceProvider.GetRequiredService<EmailMarketingAgreementHandler>(), confirmation)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            scope.ServiceProvider
                .GetRequiredService<ILogger<EmailMarketingAgreementModule>>()
                .LogError(new EventId(0, "EMAIL_MARKETING_AGREEMENT_SYNC"), ex, "Email marketing agreement not synced for subscription confirmation {ConfirmationID}. The reconciliation task will retry.", confirmation.EmailSubscriptionConfirmationID);
        }
    }
}
