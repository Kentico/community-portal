using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.EmailLibrary.Internal;
using CMS.EmailMarketing;
using CMS.EmailMarketing.Internal;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Emails;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Xperience.Admin.DigitalMarketing.Internal;

namespace Kentico.Community.Portal.Web.Features.Members;

public class MemberEmailConfiguration
{
    public Dictionary<string, string> ContextItems { get; }
    public string EmailConfigurationName { get; }

    public static MemberEmailConfiguration RegistrationConfirmation(string confirmationURL) =>
        new(
            new() { { "TOKEN_ConfirmationURL", confirmationURL } },
            SystemEmails.RegistrationConfirmation.EmailConfigurationName);

    public static MemberEmailConfiguration ResetPassword(string resetURL) =>
        new(
            new() { { "TOKEN_PasswordResetURL", resetURL } },
            SystemEmails.PasswordRecoveryConfirmation.EmailConfigurationName);

    private MemberEmailConfiguration(Dictionary<string, string> contextItems, string configurationName)
    {
        ContextItems = contextItems;
        EmailConfigurationName = configurationName;
    }
}

public interface IMemberEmailService
{
    public Task SendEmail(CommunityMember member, MemberEmailConfiguration configuration);
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
    IEmailUnsubscriptionUrlGenerator emailUnsubscriptionUrlGenerator
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

    public async Task SendEmail(CommunityMember member, MemberEmailConfiguration configuration)
    {
        var recipient = new Recipient
        {
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email
        };

        var dataContext = new CustomTokenValueDataContext
        {
            Recipient = recipient,
            Items = configuration.ContextItems
        };


        var emailConfig = await emailConfigurationProvider
            .GetAsync(configuration.EmailConfigurationName);
        string unsubscriptionUrl = await emailUnsubscriptionUrlGenerator.GenerateSignedUrl(emailConfig, "/Kentico.Emails/Unsubscribe", recipient.Email, Guid.NewGuid(), false);
        var builderContext = new RecipientEmailMarkupBuilderContext
        {
            EmailRecipientContext = new EmailRecipientContext
            {
                FirstName = recipient.FirstName,
                LastName = recipient.LastName,
                EmailAddress = recipient.Email,
                UnsubscriptionUrl = unsubscriptionUrl
            }
        };
        var markupBuilder = await markupBuilderFactory.Create(emailConfig);
        string mergedTemplate = await markupBuilder
            .BuildEmailForSending(emailConfig, builderContext);

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
}
