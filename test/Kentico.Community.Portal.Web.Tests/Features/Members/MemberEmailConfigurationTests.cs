using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Members;

namespace Kentico.Community.Portal.Web.Tests.Features.Members;

public class MemberEmailConfigurationTests
{
    [Test]
    public void RegistrationConfirmation_Includes_OptIn_Line_When_Newsletter_Requested()
    {
        var sut = MemberEmailConfiguration.RegistrationConfirmation("https://site/confirm", newsletterOptInRequested: true);

        Assert.That(sut.ContextItems["TOKEN_ConfirmationURL"], Is.EqualTo("https://site/confirm"));
        Assert.That(sut.ContextItems["TOKEN_NewsletterOptInLine"], Is.EqualTo(MemberEmailConfiguration.NEWSLETTER_OPT_IN_LINE));
    }

    [Test]
    public void RegistrationConfirmation_Empties_OptIn_Line_When_Newsletter_Not_Requested()
    {
        // The token must still be replaced, or the email shows the raw token
        var sut = MemberEmailConfiguration.RegistrationConfirmation("https://site/confirm", newsletterOptInRequested: false);

        Assert.That(sut.ContextItems["TOKEN_NewsletterOptInLine"], Is.Empty);
    }

    [Test]
    public void EmailChangeVerification_Uses_Verification_Email_With_Change_Message()
    {
        var sut = MemberEmailConfiguration.EmailChangeVerification("https://site/member-email/confirm-change");

        Assert.That(sut.EmailConfigurationName, Is.EqualTo(SystemEmails.MemberEmailVerification.EmailConfigurationName));
        Assert.That(sut.ContextItems["TOKEN_VerificationURL"], Is.EqualTo("https://site/member-email/confirm-change"));
        Assert.That(sut.ContextItems["TOKEN_VerificationMessage"], Is.EqualTo(MemberEmailConfiguration.EMAIL_CHANGE_VERIFICATION_MESSAGE));
    }

    [Test]
    public void AlternateEmailVerification_Uses_Verification_Email_With_Alternate_Message()
    {
        var sut = MemberEmailConfiguration.AlternateEmailVerification("https://site/member-email/confirm-alternate");

        Assert.That(sut.EmailConfigurationName, Is.EqualTo(SystemEmails.MemberEmailVerification.EmailConfigurationName));
        Assert.That(sut.ContextItems["TOKEN_VerificationMessage"], Is.EqualTo(MemberEmailConfiguration.ALTERNATE_EMAIL_VERIFICATION_MESSAGE));
    }

    [Test]
    public void EmailChangedNotification_Sets_All_Tokens()
    {
        var sut = MemberEmailConfiguration.EmailChangedNotification("jane", "j***@example.com", "https://site/support/feedback");

        Assert.That(sut.EmailConfigurationName, Is.EqualTo(SystemEmails.MemberEmailChangedNotification.EmailConfigurationName));
        Assert.That(sut.ContextItems["TOKEN_Username"], Is.EqualTo("jane"));
        Assert.That(sut.ContextItems["TOKEN_NewEmail"], Is.EqualTo("j***@example.com"));
        Assert.That(sut.ContextItems["TOKEN_SupportURL"], Is.EqualTo("https://site/support/feedback"));
    }

    [TestCase("jane.doe@example.com", "j***@example.com")]
    [TestCase("j@example.com", "j***@example.com")]
    [TestCase("not-an-email", "***")]
    [TestCase("@example.com", "***")]
    public void MaskEmail_Hides_Local_Part(string email, string expected) =>
        Assert.That(MemberEmailConfiguration.MaskEmail(email), Is.EqualTo(expected));
}
