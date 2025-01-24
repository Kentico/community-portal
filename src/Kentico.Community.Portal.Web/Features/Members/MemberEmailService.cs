using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.EmailLibrary.Internal;
using CMS.EmailMarketing.Internal;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Xperience.Admin.DigitalMarketing.Internal;

namespace Kentico.Community.Portal.Web.Features.Members;

public class MemberEmailConfiguration
{
    public Dictionary<string, string> ContextItems { get; }
    public string EmailConfigurationName { get; }

    public static MemberEmailConfiguration RegistrationConfirmation(string confirmationURL) =>
        new(new() { { "TOKEN_ConfirmationURL", confirmationURL } }, "MemberRegistrationEmailConfirmation-6wjw8hge");

    public static MemberEmailConfiguration ResetPassword(string resetURL) =>
        new(new() { { "TOKEN_PasswordResetURL", resetURL } }, "MemberResetPasswordConfirmation-ahw9v8cj");

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
    IEmailContentResolver emailContentResolver,
    IEmailService emailService,
    IEmailTemplateMergeService mergeService,
    IEmailChannelLanguageRetriever languageRetriever,
    IContentItemDataInfoRetriever dataRetriever,
    IEmailChannelSenderEmailProvider senderInfoProvider,
    IInfoProvider<ContentItemInfo> contentItems,
    IInfoProvider<EmailChannelInfo> emailChannels,
    IInfoProvider<EmailChannelSenderInfo> emailChannelSenders
) : IMemberEmailService
{
    private readonly IInfoProvider<EmailConfigurationInfo> emailConfigurationProvider = emailConfigurationProvider;
    private readonly IEmailContentResolver emailContentResolver = emailContentResolver;
    private readonly IEmailService emailService = emailService;
    private readonly IEmailTemplateMergeService mergeService = mergeService;
    private readonly IEmailChannelLanguageRetriever languageRetriever = languageRetriever;
    private readonly IContentItemDataInfoRetriever dataRetriever = dataRetriever;
    private readonly IEmailChannelSenderEmailProvider senderInfoProvider = senderInfoProvider;
    private readonly IInfoProvider<ContentItemInfo> contentItems = contentItems;
    private readonly IInfoProvider<EmailChannelInfo> emailChannels = emailChannels;
    private readonly IInfoProvider<EmailChannelSenderInfo> emailChannelSenders = emailChannelSenders;

    public async Task SendEmail(CommunityMember member, MemberEmailConfiguration configuration)
    {
        var recipient = new Recipient
        {
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email
        };

        var dataContext = new CustomValueDataContext
        {
            Recipient = recipient,
            Items = configuration.ContextItems
        };

        var emailConfig = await emailConfigurationProvider
            .GetAsync(configuration.EmailConfigurationName);

        string mergedTemplate = await mergeService
            .GetMergedTemplateWithEmailData(emailConfig, false);

        string emailBody = await emailContentResolver.Resolve(
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
            PlainTextBody = emailFieldValues.EmailPlainText,
            EmailConfigurationID = emailConfig.EmailConfigurationID,
            MailoutGuid = dataContext.MailoutGuid
        };

        await emailService.SendEmail(emailMessage);
    }
}

public class CustomValueDataContext : FormAutoresponderEmailDataContext
{
    public Dictionary<string, string> Items { get; set; } = [];
}

public class CustomValueFilter : IEmailContentFilter
{
    public Task<string> Apply(
        string text,
        EmailConfigurationInfo email,
        IEmailDataContext dataContext)
    {
        if (dataContext is CustomValueDataContext customValueContext)
        {
            foreach (var (key, val) in customValueContext.Items)
            {
                text = text.Replace(key, val);
            }
        }

        return Task.FromResult(text);
    }
}
