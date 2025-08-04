using CMS.ContactManagement;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.EmailLibrary.Internal;
using CMS.EmailMarketing;
using CMS.EmailMarketing.Internal;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Emails;
using Kentico.Xperience.Admin.DigitalMarketing.Internal;

namespace Kentico.Community.Portal.Web.Features.Support;

public interface ISupportEmailSender
{
    /// <summary>
    /// Sends a request received confirmation email to the support request author
    /// </summary>
    /// <param name="supportRequest"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendConfirmationEmail(SupportRequestMessage message, CancellationToken cancellationToken);
}

public class SupportEmailSender(
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
    IEventLogService log
) : ISupportEmailSender
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
    private readonly IEventLogService log = log;

    /// <inheritdoc />
    public async Task SendConfirmationEmail(SupportRequestMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var recipient = new Recipient
            {
                FirstName = message.FirstName,
                LastName = message.LastName,
                Email = message.Email
            };

            var dataContext = new CustomTokenValueDataContext
            {
                Recipient = recipient,
                Items = new() { { "TOKEN_SUPPORT_REQUEST_SUBJECT", message.Subject } }
            };

            var emailConfig = await emailConfigurationProvider
                .GetAsync(SystemEmails.SupportRequestConfirmation.EmailConfigurationName);
            string unsubscriptionUrl = await emailUnsubscriptionUrlGenerator.GenerateSignedUrl(emailConfig, "/Kentico.Emails/Unsubscribe", recipient.Email, Guid.NewGuid(), false);
            /* var builderContext = new RecipientEmailMarkupBuilderContext
            {
                EmailRecipientContext = new EmailRecipientContext
                {
                    FirstName = recipient.FirstName,
                    LastName = recipient.LastName,
                    EmailAddress = recipient.Email,
                    UnsubscriptionUrl = unsubscriptionUrl
                }
            }; */
            var currentContact = ContactManagementContext.CurrentContact;

            var markupBuilder = await markupBuilderFactory.Create(emailConfig);
            string mergedTemplate = await markupBuilder.BuildEmailForSending(emailConfig, SetEmailContext(dataContext.MailoutGuid, currentContact, recipient.Email, emailConfig));

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
            string plainTextBody = await contentResolver.Resolve(
                emailConfig,
                emailFieldValues.EmailPlainText,
                EmailContentFilterType.Sending,
                dataContext);

            var emailChannel = (await emailChannels.Get()
                .WhereEquals(
                    nameof(EmailChannelInfo.EmailChannelID),
                    emailConfig.EmailConfigurationEmailChannelID)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (emailChannel is null)
            {
                throw new Exception($"There is not email channel for the email configuration [{emailConfig.EmailConfigurationID}]");
            }

            var sender = await emailChannelSenders
                .GetAsync(emailFieldValues.EmailSenderID);
            string senderEmail = await senderInfoProvider
                .GetEmailAddress(emailChannel.EmailChannelID, sender.EmailChannelSenderName);

            var emailMessage = new EmailMessage
            {
                From = $"\"{sender.EmailChannelSenderDisplayName}\" <{senderEmail}>",
                Recipients = recipient.Email,
                Subject = emailFieldValues.EmailSubject,
                Body = emailBody,
                PlainTextBody = plainTextBody,
                EmailConfigurationID = emailConfig.EmailConfigurationID,
                MailoutGuid = dataContext.MailoutGuid
            };

            await emailService.SendEmail(emailMessage);
        }
        catch (Exception ex)
        {
            log.LogException(
                nameof(SupportEmailSender),
                "SEND_CONFIRMATION_EMAIL",
                ex,
                $"""
                Could not send confirmation email.
                Recipient: {message.Email}
                Request: {message.Subject}
                """);
        }
    }

    private Func<IServiceProvider, Task> SetEmailContext(Guid mailoutGuid, ContactInfo currentContact, string contactEmail, EmailConfigurationInfo emailConfiguration) =>
        async (serviceProvider) =>
        {
            var recipientContextAccessor = serviceProvider.GetRequiredService<IEmailRecipientContextAccessor>();
            var emailRecipientContextProvider = serviceProvider.GetRequiredService<IEmailRecipientContextProvider>();
            var recipientContactGroupContextAccessor = serviceProvider.GetRequiredService<IEmailRecipientContactGroupContextAccessor>();

            var emailRecipientContext = await emailRecipientContextProvider.Get(currentContact.ContactID, contactEmail, emailConfiguration, mailoutGuid, default);

            recipientContextAccessor.SetContext(emailRecipientContext);

            var groupNames = currentContact.ContactGroups.Select(g => g.ContactGroupName).ToList();
            var recipientContactGroupContext = new EmailRecipientContactGroupContext() { ContactGroupNames = groupNames };

            var recipientContactGroupAccessor = serviceProvider.GetRequiredService<IEmailRecipientContactGroupContextAccessor>();

            if (recipientContactGroupAccessor is not EmailRecipientContactGroupContextAccessor accessor)
            {
                return;
            }

            accessor.Context = recipientContactGroupContext;
        };
}
