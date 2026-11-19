using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;
using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Tests.Components.PageBuilder.Widgets.Registration;

public class RegisterFormContentTests
{
    [Test]
    public void Defaults_Render_The_Previous_View_Text()
    {
        var sut = new RegisterFormContent(new RegisterWidgetProperties(), new MarkdownRenderer());

        Assert.Multiple(() =>
        {
            Assert.That(sut.TermsLabel.Value, Is.EqualTo(
                """I agree to the <a href="/terms" target="_blank">Terms of Use</a> and have read the <a href="/privacy" target="_blank">Privacy notice</a>."""));
            Assert.That(sut.NewsletterHelpText.Value!.Trim(), Is.EqualTo(
                "<p>We'll subscribe you when you confirm your email address.</p>"));
            Assert.That(sut.CookieNote.Value!.Trim(), Is.EqualTo(
                """<p>Membership works with necessary cookies only. Analytics and tracking are optional, in <a href="/cookies-policy#cookie-settings">Cookie settings</a>.</p>"""));
        });
    }

    [Test]
    public void Empty_Properties_Fall_Back_To_Defaults()
    {
        var props = new RegisterWidgetProperties
        {
            TermsLabel = " ",
            NewsletterHelpText = "",
            CookieNote = null!,
        };
        var renderer = new MarkdownRenderer();

        var sut = new RegisterFormContent(props, renderer);
        var defaults = new RegisterFormContent(new RegisterWidgetProperties(), renderer);

        Assert.Multiple(() =>
        {
            Assert.That(sut.TermsLabel.Value, Is.EqualTo(defaults.TermsLabel.Value));
            Assert.That(sut.NewsletterHelpText.Value, Is.EqualTo(defaults.NewsletterHelpText.Value));
            Assert.That(sut.CookieNote.Value, Is.EqualTo(defaults.CookieNote.Value));
        });
    }

    [Test]
    public void Edited_Properties_Are_Rendered()
    {
        var props = new RegisterWidgetProperties
        {
            TermsLabel = "I accept the [Terms](/terms).",
            NewsletterHelpText = "Confirm to subscribe.",
            CookieNote = "No cookies needed.",
        };

        var sut = new RegisterFormContent(props, new MarkdownRenderer());

        Assert.Multiple(() =>
        {
            Assert.That(sut.TermsLabel.Value, Is.EqualTo("""I accept the <a href="/terms">Terms</a>."""));
            Assert.That(sut.NewsletterHelpText.Value!.Trim(), Is.EqualTo("<p>Confirm to subscribe.</p>"));
            Assert.That(sut.CookieNote.Value!.Trim(), Is.EqualTo("<p>No cookies needed.</p>"));
        });
    }
}
