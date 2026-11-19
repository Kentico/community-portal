using Kentico.Community.Portal.Web.Features.Members.EmailVerification;

namespace Kentico.Community.Portal.Web.Tests.Features.Members;

public class MemberAlternateEmailRulesTests
{
    private static readonly DateTime now = new(2026, 11, 18, 10, 0, 0);
    private static readonly DateTime verified = new(2026, 1, 1);

    [Test]
    public void Changed_Email_Is_Verified_Now()
    {
        var (email, date) = MemberAlternateEmailRules.ResolveTrustedWrite("old@agency.com", verified, " new@agency.com ", now);

        Assert.Multiple(() =>
        {
            Assert.That(email, Is.EqualTo("new@agency.com"));
            Assert.That(date, Is.EqualTo(now));
        });
    }

    [Test]
    public void New_Email_Is_Verified_Now() =>
        Assert.That(MemberAlternateEmailRules.ResolveTrustedWrite("", null, "new@agency.com", now).VerifiedDate, Is.EqualTo(now));

    [TestCase("")]
    [TestCase("  ")]
    [TestCase(null)]
    public void Cleared_Email_Clears_Date(string? newEmail)
    {
        var (email, date) = MemberAlternateEmailRules.ResolveTrustedWrite("old@agency.com", verified, newEmail, now);

        Assert.Multiple(() =>
        {
            Assert.That(email, Is.Empty);
            Assert.That(date, Is.Null);
        });
    }

    [TestCase("old@agency.com")]
    [TestCase("OLD@Agency.com ")]
    public void Unchanged_Email_Keeps_Original_Date(string newEmail) =>
        Assert.That(MemberAlternateEmailRules.ResolveTrustedWrite("old@agency.com", verified, newEmail, now).VerifiedDate, Is.EqualTo(verified));

    [Test]
    public void Unchanged_Unverified_Email_Stays_Unverified() =>
        Assert.That(MemberAlternateEmailRules.ResolveTrustedWrite("old@agency.com", null, "old@agency.com", now).VerifiedDate, Is.Null);

    [Test]
    public void Self_Service_Scope_Is_Nestable_And_Ends_On_Dispose()
    {
        Assert.That(MemberAlternateEmailRules.IsSelfServiceWrite, Is.False);

        using (MemberAlternateEmailRules.BeginSelfServiceWrite())
        {
            using (MemberAlternateEmailRules.BeginSelfServiceWrite())
            {
                Assert.That(MemberAlternateEmailRules.IsSelfServiceWrite, Is.True);
            }

            Assert.That(MemberAlternateEmailRules.IsSelfServiceWrite, Is.True);
        }

        Assert.That(MemberAlternateEmailRules.IsSelfServiceWrite, Is.False);
    }
}
