using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Tests.Membership;

public class CommunityMemberSignInStateTests
{
    private static readonly DateTime confirmed = new(2026, 1, 1);

    [TestCase(true, false, MemberSignInState.Active)]
    [TestCase(true, true, MemberSignInState.Active)]
    [TestCase(false, false, MemberSignInState.EmailNotConfirmed)]
    [TestCase(false, true, MemberSignInState.Disabled)]
    public void SignInState_Follows_Enabled_And_Confirmed_Date(bool enabled, bool hasConfirmedDate, MemberSignInState expected)
    {
        var member = new CommunityMember
        {
            Enabled = enabled,
            EmailConfirmedDate = hasConfirmedDate ? confirmed : null,
        };

        Assert.That(member.SignInState, Is.EqualTo(expected));
    }

    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Moderation_Wins_Over_Every_Other_State(bool enabled, bool hasConfirmedDate)
    {
        var member = new CommunityMember
        {
            Enabled = enabled,
            EmailConfirmedDate = hasConfirmedDate ? confirmed : null,
            ModerationStatus = ModerationStatuses.Spam,
        };

        Assert.Multiple(() =>
        {
            Assert.That(member.SignInState, Is.EqualTo(MemberSignInState.UnderModeration));
            Assert.That(member.CanConfirmEmail, Is.False);
        });
    }

    [TestCase(false, false, true, TestName = "CanConfirmEmail_Never_Confirmed")]
    [TestCase(false, true, false, TestName = "CanConfirmEmail_Not_A_Disabled_Member")]
    [TestCase(true, false, false, TestName = "CanConfirmEmail_Not_An_Enabled_Member")]
    [TestCase(true, true, false, TestName = "CanConfirmEmail_Not_A_Confirmed_Member")]
    public void CanConfirmEmail_Only_When_Never_Confirmed(bool enabled, bool hasConfirmedDate, bool expected)
    {
        var member = new CommunityMember
        {
            Enabled = enabled,
            EmailConfirmedDate = hasConfirmedDate ? confirmed : null,
        };

        Assert.That(member.CanConfirmEmail, Is.EqualTo(expected));
    }
}
