using Kentico.Community.Portal.Web.Features.DataCollection;

namespace Kentico.Community.Portal.Web.Tests.Features.DataCollection;

/// <summary>
/// Google Consent Mode commands. E2E specs check what the browser receives; these pin the
/// exact parameters.
/// </summary>
public class ConsentModeScriptTests
{
    private const string DENIED = """{"analytics_storage":"denied","ad_storage":"denied","ad_user_data":"denied","ad_personalization":"denied"}""";
    private const string ANALYTICS_GRANTED = """{"analytics_storage":"granted","ad_storage":"denied","ad_user_data":"denied","ad_personalization":"denied"}""";

    [Test]
    public void Page_Load_Without_Choice_Is_Default_Denied_Only()
    {
        string script = ConsentModeScript.ForPageLoad(null).ToString()!;

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.StartWith($"<script {ConsentModeScript.MARKER_ATTRIBUTE}>"));
            Assert.That(script, Does.Contain("window.gtag = window.gtag || function () { window.dataLayer.push(arguments); };"));
            Assert.That(script, Does.Contain($"window.gtag('consent', 'default', {DENIED});"));
            Assert.That(script, Does.Not.Contain("'update'"));
        });
    }

    [Test]
    public void Page_Load_With_Analytics_Updates_After_Default()
    {
        string script = ConsentModeScript.ForPageLoad(new ConsentCookie(Analytics: true, Tracking: false, DateTimeOffset.UtcNow)).ToString()!;

        int defaultAt = script.IndexOf($"window.gtag('consent', 'default', {DENIED});", StringComparison.Ordinal);
        int updateAt = script.IndexOf($"window.gtag('consent', 'update', {ANALYTICS_GRANTED});", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(defaultAt, Is.GreaterThan(0));
            Assert.That(updateAt, Is.GreaterThan(defaultAt));
        });
    }

    [Test]
    public void Page_Load_Without_Analytics_Does_Not_Update()
    {
        string script = ConsentModeScript.ForPageLoad(new ConsentCookie(Analytics: false, Tracking: true, DateTimeOffset.UtcNow)).ToString()!;

        Assert.That(script, Does.Not.Contain("'update'"));
    }

    [TestCase(true, ANALYTICS_GRANTED)]
    [TestCase(false, DENIED)]
    public void Choice_Updates_Analytics_And_Never_Grants_Ads(bool analytics, string expected)
    {
        string script = ConsentModeScript.ForChoice(analytics, tracking: true).ToString()!;

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain($"window.gtag('consent', 'update', {expected});"));
            Assert.That(script, Does.Not.Contain(ConsentModeScript.MARKER_ATTRIBUTE));
            Assert.That(script, Does.Contain($"\"event\":\"{ConsentModeScript.LEGACY_EVENT_UPDATE}\""));
        });
    }

    [Test]
    public void Choice_With_Analytics_Loads_Tag_Manager_Snippets_After_The_Update()
    {
        string script = ConsentModeScript.ForChoice(analytics: true, tracking: false).ToString()!;

        int updateAt = script.IndexOf($"window.gtag('consent', 'update', {ANALYTICS_GRANTED});", StringComparison.Ordinal);
        int loadAt = script.IndexOf(ConsentModeScript.LOAD_TAG_MANAGER_SNIPPETS, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(updateAt, Is.GreaterThan(0));
            Assert.That(loadAt, Is.GreaterThan(updateAt));
        });
    }

    [Test]
    public void Choice_Without_Analytics_Does_Not_Load_Tag_Manager_Snippets()
    {
        string script = ConsentModeScript.ForChoice(analytics: false, tracking: true).ToString()!;

        Assert.That(script, Does.Not.Contain("updateCodeSnippets"));
    }
}
