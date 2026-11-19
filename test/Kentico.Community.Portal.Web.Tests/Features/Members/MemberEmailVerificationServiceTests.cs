using Kentico.Community.Portal.Web.Features.Members.EmailVerification;

namespace Kentico.Community.Portal.Web.Tests.Features.Members;

public class MemberEmailVerificationServiceTests
{
    [TestCase("Jane@Agency.com", "jane@agency.com", true)]
    [TestCase(" jane@agency.com ", "jane@agency.com", true)]
    [TestCase("jane@agency.com", "jane@other.com", false)]
    [TestCase("", "", false)]
    [TestCase(null, "jane@agency.com", false)]
    public void EmailsMatch_Ignores_Case_And_Whitespace(string? a, string? b, bool expected) =>
        Assert.That(MemberEmailVerificationService.EmailsMatch(a, b), Is.EqualTo(expected));

    [Test]
    public void AlternateEmailTokenPurpose_Is_Bound_To_Normalized_Email()
    {
        // A link for one email can't verify another, and casing doesn't break a valid link
        Assert.That(
            MemberEmailVerificationService.AlternateEmailTokenPurpose(" Jane@Agency.com"),
            Is.EqualTo(MemberEmailVerificationService.AlternateEmailTokenPurpose("jane@agency.com")));
        Assert.That(
            MemberEmailVerificationService.AlternateEmailTokenPurpose("jane@agency.com"),
            Is.Not.EqualTo(MemberEmailVerificationService.AlternateEmailTokenPurpose("jane@other.com")));
    }

    [Test]
    public void SelectIntegrationMatch_Prefers_Account_Email_Over_Alternate_Email() =>
        Assert.That(
            MemberEmailVerificationService.SelectIntegrationMatch([new(3, false), new(9, true), new(1, false)]),
            Is.EqualTo(9));

    [Test]
    public void SelectIntegrationMatch_Breaks_Ties_With_Lowest_MemberID() =>
        Assert.Multiple(() =>
        {
            Assert.That(MemberEmailVerificationService.SelectIntegrationMatch([new(7, false), new(4, false)]), Is.EqualTo(4));
            Assert.That(MemberEmailVerificationService.SelectIntegrationMatch([new(8, true), new(5, true)]), Is.EqualTo(5));
        });

    [Test]
    public void SelectIntegrationMatch_Is_Null_Without_Candidates() =>
        Assert.That(MemberEmailVerificationService.SelectIntegrationMatch([]), Is.Null);
}
