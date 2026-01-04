using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using CMS.ContentEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Globally applied authorization filter which checks if the current request
/// meets the requirements of the current web page, if it has any assigned <see cref="Modules.SystemTaxonomies.ContentAuthorization"/> tags.
/// </summary>
/// <remarks>
/// Content authorization requirements are defined to satisfy the check if at least 1 tag requirement is satisfied
/// </remarks>
/// <param name="executor"></param>
/// <param name="cache"></param>
/// <param name="dataContextRetriever"></param>
/// <param name="userManager"></param>
public class ContentAuthorizationFilter(
    IContentQueryExecutor executor,
    IProgressiveCache cache,
    IWebPageDataContextRetriever dataContextRetriever,
    UserManager<CommunityMember> userManager)
    : IAsyncAuthorizationFilter
{
    private readonly IContentQueryExecutor executor = executor;
    private readonly IProgressiveCache cache = cache;
    private readonly IWebPageDataContextRetriever dataContextRetriever = dataContextRetriever;
    private readonly UserManager<CommunityMember> userManager = userManager;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        /*
         * If the request is not for a web page, skip access checks
         */
        if (!dataContextRetriever.TryRetrieve(out var data))
        {
            return;
        }

        /*
         * If the request is in the Page Builder, skip access checks
         */
        var httpContext = context.HttpContext;
        if (httpContext.Kentico().PageBuilder().EditMode || httpContext.Kentico().Preview().Enabled)
        {
            return;
        }

        /*
         * If a page has no content authorization tags, skip access checks
         */
        var tags = await GetWebPageContentAuthorizationTags(data.WebPage);
        if (tags.Count == 0)
        {
            return;
        }

        /*
         * The page has content authorization tags which require authentication
         */
        if (context.HttpContext.User.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            context.Result = new ChallengeResult();

            return;
        }

        /*
         * This should never happen, but let's check anyway
         */
        var member = await userManager.GetUserAsync(context.HttpContext.User);
        if (member is null || !member.Enabled || member.IsUnderModeration)
        {
            context.Result = new UnauthorizedResult();

            return;
        }

        /*
         * Access check business logic is manual since we don't have a role/permission system for members
         */
        if (tags.Any(t => t.Identifier.Equals(SystemTaxonomies.ContentAuthorizationTaxonomy.CommunityMemberTag.GUID))
            && identity.IsAuthenticated)
        {
            return;
        }
        if (tags.Any(t => t.Identifier.Equals(SystemTaxonomies.ContentAuthorizationTaxonomy.MVPTag.GUID)) &&
            member.ProgramStatus == ProgramStatuses.MVP)
        {
            return;
        }
        if (tags.Any(t => t.Identifier.Equals(SystemTaxonomies.ContentAuthorizationTaxonomy.CommunityLeaderTag.GUID)) &&
            member.ProgramStatus == ProgramStatuses.CommunityLeader)
        {
            return;
        }
        if (tags.Any(t => t.Identifier.Equals(SystemTaxonomies.ContentAuthorizationTaxonomy.InternalEmployeeTag.GUID)) &&
            member.IsInternalEmployee)
        {
            return;
        }

        /*
         * The member is authenticated but doesn't have the required "authorization"
         */
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        context.Result = new EmptyResult(); // Continue processing to reach StatusCodePagesWithReExecute

        return;
    }

    private async Task<ImmutableList<TagReference>> GetWebPageContentAuthorizationTags(RoutedWebPage page)
    {
        var tags = await cache.LoadAsync(async cs =>
        {
            if (cs.Cached)
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency($"webpageitem|byid|{page.WebPageItemID}");
            }

            var query = new ContentItemQueryBuilder()
                .ForContentTypes(q => q
                    .OfReusableSchema(IContentAuthorizationFields.REUSABLE_FIELD_SCHEMA_NAME)
                    .ForWebsite([page.WebPageItemID]));

            var pages = await executor.GetMappedWebPageResult<IContentAuthorizationFields>(query, new ContentQueryExecutionOptions { ForPreview = false, IncludeSecuredItems = true });
            return pages
                .SelectMany(p => p.ContentAuthorizationAllowedTags)
                .ToImmutableList();
        }, new CacheSettings(30, [nameof(ContentAuthorizationFilter), nameof(GetWebPageContentAuthorizationTags), page.WebPageItemID]));

        return tags;
    }
}
