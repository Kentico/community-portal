using CMS.ContactManagement;

namespace Kentico.Community.Portal.Core.Membership;

/// <summary>
/// The portal's rule for an active recipient list subscription, as SQL: a contact list member whose
/// latest subscription confirmation (by date, then ID) for that list is approved. Xperience adds a
/// confirmation row for every confirm and revoke, so only the latest counts.
/// </summary>
public static class ActiveSubscriptionSql
{
    public const string MEMBER_TABLE = "OM_ContactGroupMember";
    public const string CONFIRMATION_TABLE = "EmailLibrary_EmailSubscriptionConfirmation";

    /// <summary>
    /// WHERE predicate for a query that joins the member and confirmation tables on the contact ID.
    /// Pass the aliases when the query uses them.
    /// </summary>
    public static string Predicate(string memberTable = MEMBER_TABLE, string confirmationTable = CONFIRMATION_TABLE) => $"""
        {memberTable}.ContactGroupMemberType = {(int)ContactGroupMemberTypeEnum.Contact}
            AND {confirmationTable}.EmailSubscriptionConfirmationRecipientListID = {memberTable}.ContactGroupMemberContactGroupID
            AND {confirmationTable}.EmailSubscriptionConfirmationIsApproved = 1
            AND {confirmationTable}.EmailSubscriptionConfirmationID = (
                SELECT TOP 1 LatestESC.EmailSubscriptionConfirmationID
                FROM {CONFIRMATION_TABLE} LatestESC
                WHERE LatestESC.EmailSubscriptionConfirmationContactID = {confirmationTable}.EmailSubscriptionConfirmationContactID
                    AND LatestESC.EmailSubscriptionConfirmationRecipientListID = {confirmationTable}.EmailSubscriptionConfirmationRecipientListID
                ORDER BY LatestESC.EmailSubscriptionConfirmationDate DESC, LatestESC.EmailSubscriptionConfirmationID DESC)
        """;
}
