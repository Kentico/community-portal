using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules.Membership;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kentico.Community.Portal.Web.Features.Testing;

public static class E2ETestingEndpoints
{
    public static RouteGroupBuilder MapE2ETestingTools(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/testing/e2e")
            .WithTags("E2E Testing Tools")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoints used by Playwright to prepare application state for deterministic E2E tests.");

        group.MapPost(
                "/member-moderation",
                async Task<Results<Ok<E2EMemberModerationResponse>, NotFound<string>, BadRequest<string>>> (
                    E2EMemberModerationRequest request,
                    E2EMemberModerationService moderationService,
                    CancellationToken cancellationToken) =>
                {
                    if (!Enum.TryParse<ModerationStatuses>(request.ModerationStatus, ignoreCase: true, out var moderationStatus))
                    {
                        return TypedResults.BadRequest($"Unsupported moderation status '{request.ModerationStatus}'.");
                    }

                    var result = await moderationService.UpdateAsync(request.UserName, moderationStatus, cancellationToken);

                    return result is null
                        ? TypedResults.NotFound($"Member '{request.UserName}' was not found.")
                        : TypedResults.Ok(result);
                })
            .WithName("SetMemberModerationForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint used by Playwright to prepare member moderation state.");

        return group;
    }
}

public record E2EMemberModerationRequest(string UserName, string ModerationStatus);

public record E2EMemberModerationResponse(string UserName, string ModerationStatus);

public class E2EMemberModerationService(IInfoProvider<MemberInfo> memberInfoProvider)
{
    private readonly IInfoProvider<MemberInfo> memberInfoProvider = memberInfoProvider;

    public async Task<E2EMemberModerationResponse?> UpdateAsync(
        string userName,
        ModerationStatuses moderationStatus,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var members = await memberInfoProvider
            .Get()
            .WhereEquals(nameof(MemberInfo.MemberName), userName)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var member = members.FirstOrDefault();
        if (member is null)
        {
            return null;
        }

        member.MemberModerationStatus = moderationStatus.ToString();
        await memberInfoProvider.SetAsync(member);

        return new E2EMemberModerationResponse(member.MemberName, member.MemberModerationStatus);
    }
}
