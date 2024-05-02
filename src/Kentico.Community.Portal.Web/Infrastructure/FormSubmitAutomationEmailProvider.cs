using CMS;
using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;
using CMS.OnlineForms;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Newtonsoft.Json;

[assembly: RegisterImplementation(
    implementedType: typeof(IFormSubmitAutomationEmailProvider),
    implementation: typeof(FormSubmitAutomationEmailProvider),
    Priority = RegistrationPriority.SystemDefault)]

namespace Kentico.Community.Portal.Web.Infrastructure;

public class FormSubmitAutomationEmailProvider(
    IFormSubmitAutomationEmailProvider defaultAutomationEmailProvider,
    IMediator mediator) : IFormSubmitAutomationEmailProvider
{
    private readonly IFormSubmitAutomationEmailProvider defaultAutomationEmailProvider = defaultAutomationEmailProvider;
    private readonly IMediator mediator = mediator;

    public async Task<AutomationEmail?> GetEmail(BizFormInfo form, BizFormItem formData, ContactInfo contact)
    {
        string recipient = contact.ContactEmail;

        if (string.IsNullOrEmpty(recipient))
        {
            return null;
        }

        var settings = await mediator.Send(new WebsiteSettingsContentQuery());
        string formsConfiguration = settings.WebsiteSettingsContentFormsConfigurationJSON;

        if (string.IsNullOrWhiteSpace(formsConfiguration))
        {
            return await defaultAutomationEmailProvider.GetEmail(form, formData, contact);
        }

        var formsNotifications = GetNotifications(formsConfiguration);
        var email = GetEmail(form.FormName, recipient, formsNotifications);

        return email ?? await defaultAutomationEmailProvider.GetEmail(form, formData, contact);
    }

    private AutomationEmail? GetEmail(string formName, string recipient,
        Dictionary<string, EmailNotificationDto> formsConfiguration)
    {
        var configuration =
            formsConfiguration.FirstOrDefault(i => i.Key.Equals(formName, StringComparison.InvariantCultureIgnoreCase));

        if (configuration.Value == null)
        {
            return null;
        }

        return new AutomationEmail()
        {
            Sender = configuration.Value.Sender,
            Body = configuration.Value.Body,
            Subject = configuration.Value.Subject,
            Recipients = [recipient]
        };
    }

    private Dictionary<string, EmailNotificationDto> GetNotifications(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, EmailNotificationDto>>(value) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public class EmailNotificationDto
{
    public string Subject { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Body { get; set; } = "";
}
