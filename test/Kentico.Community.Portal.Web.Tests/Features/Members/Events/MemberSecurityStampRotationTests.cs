using Kentico.Community.Portal.Web.Features.Members.Events;

namespace Kentico.Community.Portal.Web.Tests.Features.Members.Events;

public class MemberSecurityStampRotationTests
{
    [Test]
    public void Enabled_Change_Rotates() =>
        Assert.That(MemberSecurityStampRotation.ShouldRotate(["MemberEnabled"]), Is.True);

    [Test]
    public void ModerationStatus_Change_Rotates() =>
        Assert.That(MemberSecurityStampRotation.ShouldRotate(["MemberFirstName", "MemberModerationStatus"]), Is.True);

    [Test]
    public void Unrelated_Changes_Do_Not_Rotate() =>
        Assert.That(MemberSecurityStampRotation.ShouldRotate(["MemberFirstName", "MemberSecurityStamp"]), Is.False);

    [Test]
    public void No_Changes_Do_Not_Rotate() =>
        Assert.That(MemberSecurityStampRotation.ShouldRotate([]), Is.False);
}
