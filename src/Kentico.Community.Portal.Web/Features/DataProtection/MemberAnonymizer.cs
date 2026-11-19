using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Features.QAndA.Search;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <summary>
/// Erasure of a member account: disable, archive (the moderation status says why it's disabled) and
/// anonymize. The member is never
/// deleted, so Q&amp;A, badges and program form rows keep a valid member ID and are attributed to
/// "Former member".
/// </summary>
/// <remarks>
/// Erasure finds the member by email, so steps that can fail (avatar deletion: permissions,
/// references) run before the email is replaced. A failure throws, the Data protection app reports
/// the erasure as failed, and running it again finds the member and retries. Only the Q&amp;A index
/// rebuild runs after the identity is gone, because the index must pick up the new name.
/// </remarks>
public class MemberAnonymizer(
    IInfoProvider<MemberInfo> memberProvider,
    IInfoProvider<MemberExternalLoginInfo> externalLoginProvider,
    IInfoProvider<QAndAAnswerDataInfo> answerProvider,
    MemberContentItemsQuery contentItemsQuery,
    ContentItemManagerCreator contentItemManagerCreator,
    ILuceneClient luceneClient)
{
    public const string FORMER_MEMBER_NAME = "Former member";

    /// <summary>Profile columns cleared on erasure.</summary>
    private static readonly string[] clearedColumns =
    [
        "MemberLastName",
        "MemberLinkedInIdentifier",
        "MemberAvatarFileExtension",
        "MemberEmployerLink",
        "MemberJobTitle",
        "MemberCountry",
        "MemberBio",
        "MemberTimeZone",
        "MemberAdministratorNotes",
        "MemberAlternateEmail",
    ];

    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IInfoProvider<MemberExternalLoginInfo> externalLoginProvider = externalLoginProvider;
    private readonly IInfoProvider<QAndAAnswerDataInfo> answerProvider = answerProvider;
    private readonly MemberContentItemsQuery contentItemsQuery = contentItemsQuery;
    private readonly ContentItemManagerCreator contentItemManagerCreator = contentItemManagerCreator;
    private readonly ILuceneClient luceneClient = luceneClient;

    public static string FormerMemberUserName(int memberID) => $"former-member-{memberID}";

    /// <exception cref="PersonalDataErasureException">A step failed. The message says whether re-running the erasure retries it.</exception>
    public async Task Anonymize(MemberInfo member, CancellationToken cancellationToken)
    {
        int memberID = member.MemberID;

        // 1. While the member is still findable by email
        await DeleteAvatars(memberID, cancellationToken);

        externalLoginProvider.BulkDelete(new WhereCondition()
            .WhereEquals(nameof(MemberExternalLoginInfo.MemberExternalLoginMemberID), memberID));

        // 2. The identity
        member.MemberEmail = ErasureOptionKeys.ErasedEmail(memberID);
        member.MemberName = FormerMemberUserName(memberID);
        member.MemberPassword = "";
        member.MemberSecurityStamp = Guid.NewGuid().ToString();
        member.MemberEnabled = false;
        member.SetValue("MemberFirstName", FORMER_MEMBER_NAME);
        member.SetValue("MemberModerationStatus", nameof(ModerationStatuses.Archived));
        member.SetValue("MemberNewsletterOptInRequestedDate", null);
        if (member.ContainsColumn("MemberEmailConfirmedDate"))
        {
            member.SetValue("MemberEmailConfirmedDate", null);
        }
        if (member.ContainsColumn("MemberAlternateEmailVerifiedDate"))
        {
            member.SetValue("MemberAlternateEmailVerifiedDate", null);
        }

        foreach (string column in clearedColumns)
        {
            if (member.ContainsColumn(column))
            {
                member.SetValue(column, "");
            }
        }

        memberProvider.Set(member);

        // 3. Needs the new name; erasure by email can't retry it
        try
        {
            await ReindexQAndA(memberID);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new PersonalDataErasureException(
                $"Member {memberID} was anonymized, but rebuilding the '{QAndASearchIndexModel.IndexName}' search index failed, so the index can still show the former name. " +
                "Rebuild the index in the Search app; running the erasure again won't find the member.",
                ex);
        }
    }

    /// <summary>
    /// The avatar is the member's own upload, so it goes. Other content items are editorial. Throws
    /// on the first failure, before the member is anonymized.
    /// </summary>
    protected virtual async Task DeleteAvatars(int memberID, CancellationToken cancellationToken)
    {
        var avatars = contentItemsQuery.Get([memberID], MemberAssetContent.CONTENT_TYPE_NAME);
        if (avatars.Count == 0)
        {
            return;
        }

        var manager = await contentItemManagerCreator.GetContentItemManager();

        foreach (var avatar in avatars)
        {
            try
            {
                await manager.Delete(new DeleteContentItemParameters(avatar.ContentItemID, avatar.LanguageName) { Permanently = true }, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new PersonalDataErasureException(
                    $"Could not delete avatar content item {avatar.ContentItemID} ({avatar.LanguageName}) of member {memberID}. " +
                    "The member was not anonymized. Fix the cause (for example, remove references to the item in Content hub) and run the erasure again.",
                    ex);
            }
        }
    }

    /// <summary>The Q&amp;A index stores author names, so it's rebuilt when the member wrote Q&amp;A content.</summary>
    private async Task ReindexQAndA(int memberID)
    {
        bool hasQuestions = contentItemsQuery.Get([memberID], QAndAQuestionPage.CONTENT_TYPE_NAME).Count > 0;
        bool hasAnswers = hasQuestions || answerProvider.Get()
            .WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID), memberID)
            .TopN(1)
            .Any();

        if (hasAnswers)
        {
            await luceneClient.Rebuild(QAndASearchIndexModel.IndexName, null);
        }
    }
}
