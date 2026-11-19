using Kentico.Community.Portal.Web.Features.Members.EmailVerification;

namespace Kentico.Community.Portal.Web.Tests.Features.Members;

public class MemberEmailConfirmedRulesTests
{
    private static readonly DateTime now = new(2026, 11, 18, 10, 0, 0);
    private static readonly DateTime confirmed = new(2026, 1, 1);

    [Test]
    public void Becoming_Enabled_Without_A_Date_Sets_Now() =>
        Assert.That(MemberEmailConfirmedRules.Resolve(wasEnabled: false, isEnabled: true, null, now), Is.EqualTo(now));

    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public void A_Set_Date_Is_Never_Changed(bool wasEnabled, bool isEnabled) =>
        Assert.That(MemberEmailConfirmedRules.Resolve(wasEnabled, isEnabled, confirmed, now), Is.EqualTo(confirmed));

    [TestCase(false, false, TestName = "Resolve_Stays_Null_While_Disabled")]
    [TestCase(true, false, TestName = "Resolve_Stays_Null_When_Disabled")]
    [TestCase(true, true, TestName = "Resolve_Stays_Null_When_Already_Enabled")]
    public void Stays_Null_Unless_Becoming_Enabled(bool wasEnabled, bool isEnabled) =>
        Assert.That(MemberEmailConfirmedRules.Resolve(wasEnabled, isEnabled, null, now), Is.Null);
}
