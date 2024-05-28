using CMS.Core;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.Membership;
using CMS.OnlineForms;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.Forms;

public class FormInternalAutoresponderEmailSender(
    IInfoProvider<UserInfo> userProvider,
    IInfoProvider<BizFormSettingsInfo> formSettingsProvider,
    IEventLogService log,
    IPageUrlGenerator pageUrlGenerator,
    IEmailService emailService,
    IOptions<SystemDomainOptions> systemDomainOptions,
    IOptions<SystemEmailOptions> systemEmailOptions
)
{
    private readonly IInfoProvider<UserInfo> userProvider = userProvider;
    private readonly IInfoProvider<BizFormSettingsInfo> formSettingsProvider = formSettingsProvider;
    private readonly IEventLogService log = log;
    private readonly IPageUrlGenerator pageUrlGenerator = pageUrlGenerator;
    private readonly IEmailService emailService = emailService;
    private readonly SystemEmailOptions systemEmailOptions = systemEmailOptions.Value;
    private readonly SystemDomainOptions systemDomainOptions = systemDomainOptions.Value;

    public async Task SendEmail(BizFormItem formItem, BizFormInfo form)
    {
        var formSettings = await formSettingsProvider.Get()
            .WhereEquals(nameof(BizFormSettingsInfo.BizFormSettingsBizFormID), form.FormID)
            .FirstOrDefaultAsync();

        if (formSettings is null || !formSettings.BizFormSettingsInternalAutoresponderIsEnabled)
        {
            return;
        }

        string recipientEmail = await userProvider.Get()
            .WhereEquals(nameof(UserInfo.UserID), formSettings.BizFormSettingsInternalAutoresponderRecipientUserID)
            .Column(nameof(UserInfo.Email))
            .GetScalarResultAsync("");

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        string formUrl = pageUrlGenerator.GenerateUrl(typeof(FormInternalAutoresponderTab), [form.FormID.ToString()]);
        string submissionUrl = pageUrlGenerator.GenerateUrl(typeof(FormSubmissionDetails), [form.FormID.ToString(), formItem.ItemID.ToString()]);

        var emailMessage = new EmailMessage
        {
            Subject = $"New form submission: {form.FormDisplayName}",
            Recipients = recipientEmail,
            From = $"no-reply@{systemEmailOptions.SendingDomain}",
            Body = $"""
                <p>There's <a href="{GetAdminUrl(submissionUrl)}">a new form submission</a>  available for review.</p>

                <p>You have received this email because you are designated as the internal autoresponder recipient for this form.</p>
                <p>You can change these settings by <a href="{GetAdminUrl(formUrl)}">configuring the form's settings</a>.<p>
                """,
            EmailFormat = EmailFormatEnum.Html,
        };

        try
        {
            await emailService.SendEmail(emailMessage);
        }
        catch (Exception ex)
        {
            log.LogException(
                nameof(FormInternalAutoresponderEmailSender),
                "SEND_INTERNAL_AUTORESPONDER_EMAIL",
                ex,
                $"""
                Could not send confirmation email.
                Recipient: {recipientEmail}
                Request: {emailMessage.Subject}
                """);
        }
    }

    private string GetAdminUrl(string relativePath) => $"{systemDomainOptions.WebBaseUrl}/admin{relativePath}";
}

public class SystemDomainOptions
{
    public string WebBaseUrl { get; set; } = "";
}
