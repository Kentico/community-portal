using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Identity;

namespace Kentico.Community.Portal.Web.Infrastructure;

public enum QAndAQuestionPermissionType
{
    Edit,
    Delete,
    SubmitAnswer
}

public enum QAndAAnswerPermissionType
{
    Edit,
    Delete,
    MarkApprovedAnswer
}

public record QAndAQuestionPermissions(bool Edit, bool Delete, bool Answer, bool CanReact)
{
    public static QAndAQuestionPermissions NoPermissions { get; } = new(false, false, false, false);
    public bool CanInteract => Edit || Answer || Delete || CanReact;
}

public record QAndAAnswerPermissions(bool Edit, bool Delete, bool MarkApprovedAnswer, bool CanReact)
{
    public static QAndAAnswerPermissions NoPermissions { get; } = new(false, false, false, false);
    public bool CanInteract => Edit || Delete || MarkApprovedAnswer || CanReact;
}

/// <summary>
/// Service for checking content permissions for the current user
/// </summary>
public interface IQAndAPermissionService
{
    /// <summary>
    /// Checks if the current user has the specified permission for content management
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="permissionType">The type of permission to check</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    public Task<bool> HasPermission(CommunityMember? communityMember, QAndAQuestionPage question, QAndAQuestionPermissionType permissionType);
    public Task<bool> HasPermission(HttpContext httpContext, QAndAQuestionPage question, QAndAQuestionPermissionType permissionType);
    public Task<QAndAQuestionPermissions> GetPermissions(CommunityMember? communityMember, QAndAQuestionPage question);

    /// <summary>
    /// Checks if the specified community member has the specified permission for content management
    /// </summary>
    /// <param name="communityMember">The community member to check</param>
    /// <param name="permissionType">The type of permission to check</param>
    /// <returns>True if the member has the permission, false otherwise</returns>
    public Task<bool> HasPermission(CommunityMember? communityMember, QAndAQuestionPage question, QAndAAnswerDataInfo answer, QAndAAnswerPermissionType permissionType);
    public Task<bool> HasPermission(HttpContext httpContext, QAndAQuestionPage question, QAndAAnswerDataInfo answer, QAndAAnswerPermissionType permissionType);
    public Task<QAndAAnswerPermissions> GetPermissions(CommunityMember? communityMember, QAndAQuestionPage question, QAndAAnswerDataInfo answer);
}

/// <summary>
/// Implementation of content permission service
/// </summary>
public class QAndAPermissionService(
    UserManager<CommunityMember> userManager,
    IProgressiveCache cache,
    IInfoProvider<UserInfo> userInfoProvider) : IQAndAPermissionService
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IProgressiveCache cache = cache;
    private readonly IInfoProvider<UserInfo> userInfoProvider = userInfoProvider;

    /// <inheritdoc/>
    public async Task<bool> HasPermission(HttpContext httpContext, QAndAQuestionPage question, QAndAQuestionPermissionType permissionType)
    {
        var currentUser = await userManager.CurrentUser(httpContext);
        return await HasPermission(currentUser, question, permissionType);
    }

    /// <inheritdoc/>
    public async Task<bool> HasPermission(CommunityMember? communityMember, QAndAQuestionPage question, QAndAQuestionPermissionType permissionType)
    {
        if (communityMember is null)
        {
            return false;
        }

        return permissionType switch
        {
            QAndAQuestionPermissionType.Edit => MemberIsDiscussionAuthor(question, communityMember) || (await CanEditGlobally(communityMember)),
            QAndAQuestionPermissionType.Delete => QuestionIsMemberGenerated(question) && await CanDeleteGlobally(communityMember),
            QAndAQuestionPermissionType.SubmitAnswer => true, // we know the visitor is authenticated
            _ => false
        };
    }
    public async Task<QAndAQuestionPermissions> GetPermissions(CommunityMember? communityMember, QAndAQuestionPage question)
    {
        if (communityMember is null)
        {
            return QAndAQuestionPermissions.NoPermissions;
        }

        bool delete = await HasPermission(communityMember, question, QAndAQuestionPermissionType.Delete);
        bool edit = await HasPermission(communityMember, question, QAndAQuestionPermissionType.Edit);
        bool submitAnswer = await HasPermission(communityMember, question, QAndAQuestionPermissionType.SubmitAnswer);
        bool canReact = question.QAndAQuestionPageAuthorMemberID != communityMember.Id;

        return new QAndAQuestionPermissions(edit, delete, submitAnswer, canReact);
    }

    public async Task<bool> HasPermission(HttpContext httpContext, QAndAQuestionPage question, QAndAAnswerDataInfo answer, QAndAAnswerPermissionType permissionType)
    {
        var currentUser = await userManager.CurrentUser(httpContext);
        return await HasPermission(currentUser, question, answer, permissionType);
    }

    public async Task<bool> HasPermission(CommunityMember? communityMember, QAndAQuestionPage question, QAndAAnswerDataInfo answer, QAndAAnswerPermissionType permissionType)
    {
        if (communityMember is null)
        {
            return false;
        }

        return permissionType switch
        {
            QAndAAnswerPermissionType.Edit => communityMember.Id == answer.QAndAAnswerDataAuthorMemberID || (await CanEditGlobally(communityMember)),
            QAndAAnswerPermissionType.Delete => await CanDeleteGlobally(communityMember),
            QAndAAnswerPermissionType.MarkApprovedAnswer =>
                QuestionIsMemberGenerated(question)
                && AnswerIsNotCurrentAcceptedResponse(question, answer)
                && await MemberHasPermissionsToMarkAcceptedAnswer(question, communityMember),
            _ => false
        };
    }

    public static bool MemberIsDiscussionAuthor(QAndAQuestionPage question, CommunityMember communityMember) =>
        communityMember.Id == question.QAndAQuestionPageAuthorMemberID;
    private static bool QuestionIsMemberGenerated(QAndAQuestionPage question) =>
        (!question.QAndAQuestionPageBlogPostPages?.Any()) ?? false;
    private static bool AnswerIsNotCurrentAcceptedResponse(QAndAQuestionPage question, QAndAAnswerDataInfo answer) =>
        question.QAndAQuestionPageAcceptedAnswerDataGUID != answer.QAndAAnswerDataGUID;
    private async Task<bool> MemberHasPermissionsToMarkAcceptedAnswer(QAndAQuestionPage question, CommunityMember communityMember) =>
        (question.QAndAQuestionPageAcceptedAnswerDataGUID == default && question.QAndAQuestionPageAuthorMemberID == communityMember.Id)
            || await CanEditGlobally(communityMember);

    public async Task<QAndAAnswerPermissions> GetPermissions(CommunityMember? communityMember, QAndAQuestionPage question, QAndAAnswerDataInfo answer)
    {
        if (communityMember is null)
        {
            return QAndAAnswerPermissions.NoPermissions;
        }

        bool delete = await HasPermission(communityMember, question, answer, QAndAAnswerPermissionType.Delete);
        bool edit = await HasPermission(communityMember, question, answer, QAndAAnswerPermissionType.Edit);
        bool markApprovedAnswer = await HasPermission(communityMember, question, answer, QAndAAnswerPermissionType.MarkApprovedAnswer);
        bool canReact = answer.QAndAAnswerDataAuthorMemberID != communityMember.Id;

        return new QAndAAnswerPermissions(edit, delete, markApprovedAnswer, canReact);
    }

    /// <summary>
    /// Gets the CMS user corresponding to the community member
    /// </summary>
    /// <param name="communityMember">The community member</param>
    /// <returns>The corresponding CMS user or null if not found</returns>
    private async Task<Maybe<UserInfo>> GetCmsUser(CommunityMember communityMember)
    {
        var users = await cache.LoadAsync(async cs =>
            await userInfoProvider.Get()
                .WhereEquals(nameof(UserInfo.Email), communityMember.Email)
                .GetEnumerableTypedResultAsync(),
            new CacheSettings(10, nameof(GetCmsUser), communityMember.Email));

        return users.TryFirst();
    }

    /// <summary>
    /// Checks if the community member has edit permissions
    /// Edit permissions are granted to users with the Community Manager role only
    /// </summary>
    /// <param name="communityMember">The community member</param>
    /// <returns>True if the member can edit content</returns>
    private async Task<bool> CanEditGlobally(CommunityMember communityMember) =>
        await GetCmsUser(communityMember)
            .Map(user => user.IsInRole(PortalWebSiteChannel.ROLE_COMMUNITY_MANAGER))
            .GetValueOrDefault(() => false);

    /// <summary>
    /// Checks if the community member has delete permissions
    /// Delete permissions are granted to:
    /// 1. Community Managers (via role)
    /// 2. MVPs (via badge)
    /// 3. Community Leaders (via badge)
    /// </summary>
    /// <param name="communityMember">The community member</param>
    /// <returns>True if the member can delete content</returns>
    private async Task<bool> CanDeleteGlobally(CommunityMember communityMember) =>
        await GetCmsUser(communityMember)
            .Map(user => user.IsInRole(PortalWebSiteChannel.ROLE_COMMUNITY_MANAGER))
            .GetValueOrDefault(() => false)
        || communityMember.ProgramStatus == ProgramStatuses.MVP
        || communityMember.ProgramStatus == ProgramStatuses.CommunityLeader;
}
