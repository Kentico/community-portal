using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Tests.Components.PageBuilder.Widgets.CommunityProgramTargetsReport;

public class CommunityProgramMembershipResolverDetermineProgramMembershipTests
{
    private const int MvpRoleId = 1;
    private const int CommunityLeaderRoleId = 2;

    [Test]
    public void Member_With_Only_MVP_Role_Is_MVP()
    {
        var result = CommunityProgramMembershipResolver.DetermineProgramMembership(
            [(MemberId: 10, RoleId: MvpRoleId)], MvpRoleId, CommunityLeaderRoleId);

        Assert.That(result[10], Is.EqualTo(CommunityProgramMembership.MVP));
    }

    [Test]
    public void Member_With_Only_CommunityLeader_Role_Is_CommunityLeader()
    {
        var result = CommunityProgramMembershipResolver.DetermineProgramMembership(
            [(MemberId: 10, RoleId: CommunityLeaderRoleId)], MvpRoleId, CommunityLeaderRoleId);

        Assert.That(result[10], Is.EqualTo(CommunityProgramMembership.CommunityLeader));
    }

    [Test]
    public void Member_With_Both_Roles_Is_MVP()
    {
        var result = CommunityProgramMembershipResolver.DetermineProgramMembership(
            [(MemberId: 10, RoleId: CommunityLeaderRoleId), (MemberId: 10, RoleId: MvpRoleId)],
            MvpRoleId,
            CommunityLeaderRoleId);

        Assert.That(result[10], Is.EqualTo(CommunityProgramMembership.MVP));
    }

    [Test]
    public void Member_With_Both_Roles_Is_MVP_Regardless_Of_Row_Order()
    {
        var result = CommunityProgramMembershipResolver.DetermineProgramMembership(
            [(MemberId: 10, RoleId: MvpRoleId), (MemberId: 10, RoleId: CommunityLeaderRoleId)],
            MvpRoleId,
            CommunityLeaderRoleId);

        Assert.That(result[10], Is.EqualTo(CommunityProgramMembership.MVP));
    }

    [Test]
    public void Unrelated_Role_Ids_Are_Ignored()
    {
        var result = CommunityProgramMembershipResolver.DetermineProgramMembership(
            [(MemberId: 10, RoleId: 999)], MvpRoleId, CommunityLeaderRoleId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void No_Memberships_Returns_Empty_Dictionary()
    {
        var result = CommunityProgramMembershipResolver.DetermineProgramMembership(
            [], MvpRoleId, CommunityLeaderRoleId);

        Assert.That(result, Is.Empty);
    }
}
