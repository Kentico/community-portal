using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Tests.Membership;

public class TermsVersionPlaceholderTests
{
    [Test]
    public void Replaces_Every_Token_With_The_Encoded_Version() =>
        Assert.That(
            TermsVersionPlaceholder.Apply("<p>Version {{TermsVersion}}.</p><p>{{TermsVersion}}</p>", "2026-11 <b>"),
            Is.EqualTo("<p>Version 2026-11 &lt;b&gt;.</p><p>2026-11 &lt;b&gt;</p>"));

    [Test]
    public void Leaves_Html_Without_The_Token_Unchanged() =>
        Assert.That(TermsVersionPlaceholder.Apply("<p>{{Other}}</p>", "2026-11"), Is.EqualTo("<p>{{Other}}</p>"));

    [Test]
    public void Null_Html_Is_Empty() =>
        Assert.That(TermsVersionPlaceholder.Apply(null, "2026-11"), Is.Empty);
}
