using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Identity;

namespace Kentico.Community.Portal.Web.Features.Members.Events;

/// <summary>
/// Revokes the KenticoEmployee Member Role and archives the matching Member when a CMS admin
/// User is disabled or deleted, i.e. offboarding. One-directional: re-enabling the User does not
/// reverse this - only a human clears the archive. See docs/adr/0003-kenticoemployee-role-lifecycle.md.
/// </summary>
public class KenticoEmployeeOffboardingHandler(
    UserManager<CommunityMember> userManager,
    ILogger<KenticoEmployeeOffboardingHandler> logger)
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly ILogger<KenticoEmployeeOffboardingHandler> logger = logger;

    public async Task Handle(string? userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return;
        }

        var member = await userManager.FindByEmailAsync(userEmail);
        if (member is null)
        {
            return;
        }

        if (await userManager.IsInRoleAsync(member, SystemMemberRoles.KenticoEmployee.MemberRoleName))
        {
            var roleResult = await userManager.RemoveFromRoleAsync(member, SystemMemberRoles.KenticoEmployee.MemberRoleName);
            if (!roleResult.Succeeded)
            {
                logger.LogError(
                    new EventId(0, "OFFBOARD_ROLE_REMOVE_FAILED"),
                    "Failed to remove KenticoEmployee role from member {MemberId}: {Errors}",
                    member.Id,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        if (member.ModerationStatus != ModerationStatuses.Archived)
        {
            member.ModerationStatus = ModerationStatuses.Archived;
            var updateResult = await userManager.UpdateAsync(member);
            if (!updateResult.Succeeded)
            {
                logger.LogError(
                    new EventId(0, "OFFBOARD_ARCHIVE_FAILED"),
                    "Failed to archive member {MemberId}: {Errors}",
                    member.Id,
                    string.Join("; ", updateResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
