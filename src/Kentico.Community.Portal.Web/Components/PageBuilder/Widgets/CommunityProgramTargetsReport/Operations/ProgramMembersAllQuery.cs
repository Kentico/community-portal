using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport.Operations;

public record ProgramMembersAllQuery : IQuery<ProgramMembersAllQueryResponse>;
public record ProgramMembersAllQueryResponse(IReadOnlyList<CommunityMember> Members);
public class ProgramMembersAllQueryHandler(DataItemQueryTools tools, IInfoProvider<MemberInfo> memberProvider) : DataItemQueryHandler<ProgramMembersAllQuery, ProgramMembersAllQueryResponse>(tools)
{
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<ProgramMembersAllQueryResponse> Handle(ProgramMembersAllQuery request, CancellationToken cancellationToken)
    {
        var members = await memberProvider.Get()
            .WhereIn(MemberInfoExtensions.FIELD_PROGRAM_STATUS, [ProgramStatuses.CommunityLeader.ToString(), ProgramStatuses.MVP.ToString()])
            .GetEnumerableTypedResultAsync();

        return new([.. members.Select(CommunityMember.FromMemberInfo)]);
    }
    protected override ICacheDependencyKeysBuilder AddDependencyKeys(ProgramMembersAllQuery query, ProgramMembersAllQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(MemberInfo.OBJECT_TYPE);
}

public record MemberSummary(
    int MemberId,
    string DisplayName,
    string Email,
    ProgramStatuses ProgramStatus);

