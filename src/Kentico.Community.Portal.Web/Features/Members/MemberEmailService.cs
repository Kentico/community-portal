using System.ComponentModel;
using CMS.ContactManagement;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.EmailLibrary.Internal;
using CMS.EmailMarketing;
using CMS.EmailMarketing.Internal;
using EnumsNET;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Emails;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Xperience.Admin.DigitalMarketing.Internal;

namespace Kentico.Community.Portal.Web.Features.Members;

public class MemberEmailConfiguration
{
    public Dictionary<string, string> ContextItems { get; }
    public string EmailConfigurationName { get; }

    /// <summary>
    /// <c>TOKEN_NewsletterOptInLine</c> in the registration confirmation email.
    /// </summary>
    public const string NEWSLETTER_OPT_IN_LINE =
        "Confirming your email also subscribes you to Kentico Community news and updates, as you requested. You can unsubscribe at any time.";

    /// <param name="newsletterOptInRequested">The member ticked the newsletter box at registration. Adds <see cref="NEWSLETTER_OPT_IN_LINE"/>; otherwise the token is emptied.</param>
    public static MemberEmailConfiguration RegistrationConfirmation(string confirmationURL, bool newsletterOptInRequested) =>
        new(
            new()
            {
                { "TOKEN_ConfirmationURL", confirmationURL },
                { "TOKEN_NewsletterOptInLine", newsletterOptInRequested ? NEWSLETTER_OPT_IN_LINE : "" },
            },
            SystemEmails.RegistrationConfirmation.EmailConfigurationName);

    /// <summary>
    /// <c>TOKEN_VerificationMessage</c> when a member changes their account email.
    /// </summary>
    public const string EMAIL_CHANGE_VERIFICATION_MESSAGE =
        "You asked to change the email address of your Kentico Community Portal account to this address. Your account email changes only after you verify it.";

    /// <summary>
    /// <c>TOKEN_VerificationMessage</c> when a member adds an alternate email.
    /// </summary>
    public const string ALTERNATE_EMAIL_VERIFICATION_MESSAGE =
        "You asked to add this address as the alternate email of your Kentico Community Portal account.";

    /// <summary>Verification link sent to a member's requested new account email.</summary>
    public static MemberEmailConfiguration EmailChangeVerification(string verificationURL) =>
        EmailVerification(verificationURL, EMAIL_CHANGE_VERIFICATION_MESSAGE);

    /// <summary>Verification link sent to a member's alternate email.</summary>
    public static MemberEmailConfiguration AlternateEmailVerification(string verificationURL) =>
        EmailVerification(verificationURL, ALTERNATE_EMAIL_VERIFICATION_MESSAGE);

    private static MemberEmailConfiguration EmailVerification(string verificationURL, string message) =>
        new(
            new()
            {
                { "TOKEN_VerificationURL", verificationURL },
                { "TOKEN_VerificationMessage", message },
            },
            SystemEmails.MemberEmailVerification.EmailConfigurationName);

    /// <summary>
    /// Sent to the previous email after an email change.
    /// </summary>
    /// <param name="maskedNewEmail">The new email, partly hidden (<see cref="MaskEmail"/>): the old address may no longer belong to the member.</param>
    public static MemberEmailConfiguration EmailChangedNotification(string username, string maskedNewEmail, string supportURL) =>
        new(
            new()
            {
                { "TOKEN_Username", username },
                { "TOKEN_NewEmail", maskedNewEmail },
                { "TOKEN_SupportURL", supportURL },
            },
            SystemEmails.MemberEmailChangedNotification.EmailConfigurationName);

    /// <summary>
    /// Keeps the first character of the local part and the whole domain: <c>jane.doe@example.com</c> → <c>j***@example.com</c>.
    /// </summary>
    public static string MaskEmail(string email)
    {
        int at = email.IndexOf('@');
        return at <= 0
            ? "***"
            : $"{email[0]}***{email[at..]}";
    }

    public static MemberEmailConfiguration ResetPassword(string resetURL) =>
        new(
            new() { { "TOKEN_PasswordResetURL", resetURL } },
            SystemEmails.PasswordRecoveryConfirmation.EmailConfigurationName);

    public class DiscussionNotificationEmailData
    {
        public DiscussionNotificationEmailData(
            string tableHTML,
            int count,
            string baseURL,
            QAndANotificationFrequencyType frequency)
        {
            TableHTML = tableHTML;
            BaseURL = baseURL;
            Count = count.ToString();

            var enumMember = Enums.GetMember(frequency);
            Frequency = enumMember?.Attributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description
                ?? enumMember?.Name
                ?? "";
        }

        public string TableHTML { get; }
        public string BaseURL { get; }
        public string Count { get; }
        public string Frequency { get; }
    }
    public static MemberEmailConfiguration QAndADiscussionNotification(DiscussionNotificationEmailData data) =>
        new(
            new()
            {
                { "TOKEN_NotificationTableHTML", data.TableHTML },
                { "TOKEN_NotificationCount", data.Count.ToString() },
                { "TOKEN_NotificationFrequency", data.Frequency },
                { "TOKEN_BaseURL", data.BaseURL },
            },
            SystemEmails.QAndADiscussionNotification.EmailConfigurationName
        );

    private MemberEmailConfiguration(Dictionary<string, string> contextItems, string configurationName)
    {
        ContextItems = contextItems;
        EmailConfigurationName = configurationName;
    }
}

public interface IMemberEmailService
{
    /// <param name="recipientEmail">Sends to this address instead of <see cref="CommunityMember.Email"/>, e.g. an email the member is verifying.</param>
    public Task SendEmail(CommunityMember member, MemberEmailConfiguration configuration, string? recipientEmail = null);
}

public class MemberEmailService(
    IInfoProvider<EmailConfigurationInfo> emailConfigurationProvider,
    IEmailContentResolverFactory emailContentResolverFactory,
    IEmailService emailService,
    IEmailMarkupBuilderFactory markupBuilderFactory,
    IEmailChannelLanguageRetriever languageRetriever,
    IContentItemDataInfoRetriever dataRetriever,
    IEmailChannelSenderEmailProvider senderInfoProvider,
    IInfoProvider<ContentItemInfo> contentItems,
    IInfoProvider<EmailChannelInfo> emailChannels,
    IInfoProvider<EmailChannelSenderInfo> emailChannelSenders,
    IEmailUnsubscriptionUrlGenerator emailUnsubscriptionUrlGenerator,
    IInfoProvider<ContactInfo> contactProvider
) : IMemberEmailService
{
    private readonly IInfoProvider<EmailConfigurationInfo> emailConfigurationProvider = emailConfigurationProvider;
    private readonly IEmailContentResolverFactory emailContentResolverFactory = emailContentResolverFactory;
    private readonly IEmailService emailService = emailService;
    private readonly IEmailMarkupBuilderFactory markupBuilderFactory = markupBuilderFactory;
    private readonly IEmailChannelLanguageRetriever languageRetriever = languageRetriever;
    private readonly IContentItemDataInfoRetriever dataRetriever = dataRetriever;
    private readonly IEmailChannelSenderEmailProvider senderInfoProvider = senderInfoProvider;
    private readonly IInfoProvider<ContentItemInfo> contentItems = contentItems;
    private readonly IInfoProvider<EmailChannelInfo> emailChannels = emailChannels;
    private readonly IInfoProvider<EmailChannelSenderInfo> emailChannelSenders = emailChannelSenders;
    private readonly IEmailUnsubscriptionUrlGenerator emailUnsubscriptionUrlGenerator = emailUnsubscriptionUrlGenerator;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;

    public async Task SendEmail(CommunityMember member, MemberEmailConfiguration configuration, string? recipientEmail = null)
    {
        var recipient = new Recipient
        {
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = string.IsNullOrWhiteSpace(recipientEmail) ? member.Email! : recipientEmail
        };

        var dataContext = new CustomTokenValueDataContext
        {
            Recipient = recipient,
            Items = configuration.ContextItems
        };

        var emailConfig = await emailConfigurationProvider
            .GetAsync(configuration.EmailConfigurationName);
        string unsubscriptionUrl = await emailUnsubscriptionUrlGenerator.GenerateSignedUrl(emailConfig, "/Kentico.Emails/Unsubscribe", recipient.Email, Guid.NewGuid(), false);

        var currentContact = ContactManagementContext.GetCurrentContact(true)
            ?? (await contactProvider.Get().WhereEquals(nameof(ContactInfo.ContactEmail), recipient.Email).GetEnumerableTypedResultAsync()).FirstOrDefault()
            ?? new ContactInfo { ContactEmail = recipient.Email };

        var markupBuilder = await markupBuilderFactory.Create(emailConfig);
        string mergedTemplate = await markupBuilder
            .BuildEmailForSending(emailConfig, SetEmailContext(dataContext.MailoutGuid, currentContact, recipient.Email, emailConfig));

        var contentResolver = await emailContentResolverFactory.Create(emailConfig, default);
        string emailBody = await contentResolver.Resolve(
            emailConfig,
            mergedTemplate,
            EmailContentFilterType.Sending,
            dataContext);

        var contentItem = await contentItems
            .GetAsync(emailConfig.EmailConfigurationContentItemID);
        var contentLanguage = await languageRetriever
            .GetEmailChannelLanguageInfoOrThrow(emailConfig.EmailConfigurationEmailChannelID);
        var data = await dataRetriever
            .GetContentItemData(contentItem, contentLanguage.ContentLanguageID, false);
        var emailFieldValues = new EmailContentTypeSpecificFieldValues(data);

        /* Use the custom content filter directly instead of the IEmailContentResolver because the resolver adds built-in filters only meant for the email body (e.g. tracking links/pixels) 
            This manual use of the filter lets us replace tokens in content outside of the HTML body.
        */
        var tokenFilter = new CustomTokenValueEmailContentFilter();
        string subject = await tokenFilter.Apply(emailFieldValues.EmailSubject, emailConfig, dataContext);
        string plainTextBody = await tokenFilter.Apply(emailFieldValues.EmailPlainText, emailConfig, dataContext);

        var emailChannel = (await emailChannels.Get()
            .WhereEquals(
                nameof(EmailChannelInfo.EmailChannelID),
                emailConfig.EmailConfigurationEmailChannelID)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault()
            ?? throw new Exception($"There is not email channel for the email configuration [{emailConfig.EmailConfigurationID}]");

        var sender = await emailChannelSenders
            .GetAsync(emailFieldValues.EmailSenderID);
        string senderEmail = await senderInfoProvider
            .GetEmailAddress(emailChannel.EmailChannelID, sender.EmailChannelSenderName);

        var emailMessage = new EmailMessage
        {
            From = $"\"{sender.EmailChannelSenderDisplayName}\" <{senderEmail}>",
            Recipients = recipient.Email,
            Subject = subject,
            Body = emailBody,
            PlainTextBody = plainTextBody,
            EmailConfigurationID = emailConfig.EmailConfigurationID,
            MailoutGuid = dataContext.MailoutGuid
        };

        await emailService.SendEmail(emailMessage);
    }

    private static Func<IServiceProvider, Task> SetEmailContext(Guid mailoutGuid, ContactInfo currentContact, string contactEmail, EmailConfigurationInfo emailConfiguration) =>
        async (serviceProvider) =>
        {
            var emailRecipientContextProvider = serviceProvider.GetRequiredService<IEmailRecipientContextProvider>();
            var recipientContextAccessor = serviceProvider.GetRequiredService<IEmailRecipientContextAccessor>();
            var recipientContactGroupAccessor = serviceProvider.GetRequiredService<IEmailRecipientContactGroupContextAccessor>();

            var emailRecipientContext = await emailRecipientContextProvider.Get(currentContact.ContactID, contactEmail, emailConfiguration, mailoutGuid, default);

            recipientContextAccessor.SetContext(emailRecipientContext);

            if (recipientContactGroupAccessor is not EmailRecipientContactGroupContextAccessor accessor)
            {
                return;
            }

            var groupNames = currentContact.ContactGroups.Select(g => g.ContactGroupName).ToList();
            var recipientContactGroupContext = new EmailRecipientContactGroupContext() { ContactGroupNames = groupNames };
            accessor.Context = recipientContactGroupContext;
        };
}
