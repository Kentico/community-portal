using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport.Operations;

public record ProgramMembersAllQuery : IQuery<ProgramMembersAllQueryResponse>;
public record ProgramMembersAllQueryResponse(IReadOnlyList<MemberSummary> Members);

public class ProgramMembersAllQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<MemberInfo> memberProvider,
    IInfoProvider<MemberRoleInfo> memberRoleProvider,
    IInfoProvider<MemberRoleMemberInfo> memberRoleMemberProvider) : DataItemQueryHandler<ProgramMembersAllQuery, ProgramMembersAllQueryResponse>(tools)
{
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IInfoProvider<MemberRoleInfo> memberRoleProvider = memberRoleProvider;
    private readonly IInfoProvider<MemberRoleMemberInfo> memberRoleMemberProvider = memberRoleMemberProvider;

    public override async Task<ProgramMembersAllQueryResponse> Handle(ProgramMembersAllQuery request, CancellationToken cancellationToken)
    {
        var membershipByMemberId = await CommunityProgramMembershipResolver.GetMembershipByMemberId(
            memberRoleProvider, memberRoleMemberProvider, memberIds: null, cancellationToken);

        if (membershipByMemberId.Count == 0)
        {
            return new([]);
        }

        var members = await memberProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberID), membershipByMemberId.Keys.ToList())
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return new(
        [
            .. members
                .Select(m => new MemberSummary(
                    m.MemberID,
                    CommunityMember.FromMemberInfo(m).DisplayName,
                    m.MemberEmail ?? "",
                    membershipByMemberId.GetValueOrDefault(m.MemberID)))
                .OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
        ]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(ProgramMembersAllQuery query, ProgramMembersAllQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllObjects(MemberInfo.OBJECT_TYPE)
            .AllObjects(MemberRoleMemberInfo.OBJECT_TYPE);
}

public record MemberSummary(
    int MemberId,
    string DisplayName,
    string Email,
    CommunityProgramMembership Membership);
