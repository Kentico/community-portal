using System.Net.Mail;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Support;

public interface ISupportEmailSender
{
    /// <summary>
    /// Sends a request received confirmation email to the support request author
    /// </summary>
    /// <param name="supportRequest"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SendConfirmationEmail(SupportRequestMessage message, CancellationToken cancellationToken);
}

public class SupportEmailSender(
    IInfoProvider<EmailConfigurationInfo> emailConfigurationProvider,
    IInfoProvider<EmailChannelInfo> emailChannelProvider,
    IInfoProvider<EmailChannelSenderInfo> senderProvider,
    IEmailChannelDomainProvider domainProvider,
    IEmailService emailService,
    IEventLogService log
) : ISupportEmailSender
{
    private readonly IInfoProvider<EmailConfigurationInfo> emailConfigurationProvider = emailConfigurationProvider;
    private readonly IInfoProvider<EmailChannelInfo> emailChannelProvider = emailChannelProvider;
    private readonly IInfoProvider<EmailChannelSenderInfo> senderProvider = senderProvider;
    private readonly IEmailChannelDomainProvider domainProvider = domainProvider;
    private readonly IEmailService emailService = emailService;
    private readonly IEventLogService log = log;

    /// <inheritdoc />
    public async Task SendConfirmationEmail(SupportRequestMessage message, CancellationToken cancellationToken)
    {
        // TODO - in the future use the email's content to populate this email
        // which requires using some pubternal APIs
        // https://community.kentico.com/blog/programmatically-sending-templated-emails-with-dynamic-data

        try
        {
            var config = await emailConfigurationProvider.GetAsync(SystemEmails.SupportRequestConfirmationEmail.CodeName, cancellationToken);
            var emailChannel = await emailChannelProvider.GetAsync(config.EmailConfigurationEmailChannelID, cancellationToken);
            var sender = await senderProvider.Get()
                .WhereEquals(nameof(EmailChannelSenderInfo.EmailChannelSenderEmailChannelID), emailChannel.EmailChannelID)
                .SingleAsync(cancellationToken);

            var domains = await domainProvider.GetDomains(emailChannel.EmailChannelID, cancellationToken);

            var address = new MailAddress($"{sender.EmailChannelSenderName}@{domains.SendingDomain}", sender.EmailChannelSenderDisplayName);

            string body = $"""
            <p>{message.FirstName},</p>
            <p>We've begun to process your support request.</p>
            <p>Subject: {message.Subject}</p>
            <br>
            <p>Thanks,
                <br>
                <em>Kentio Community Portal</em>
            </p>
            """;

            var email = new EmailMessage()
            {
                From = address.ToString(),
                Recipients = message.Email,
                Subject = $"Kentico Community Portal: Support request received",
                Body = body,
                PlainTextBody = body,
                EmailFormat = EmailFormatEnum.Both,
                EmailConfigurationID = config.EmailConfigurationID
            };

            await emailService.SendEmail(email);
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
}
