using System.Globalization;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Membership;

namespace Kentico.Community.Portal.Web.Features.DataProtection.Retention;

/// <summary>
/// WHERE conditions over <c>OM_Contact</c> for the retention tasks. Dates are written
/// as ISO literals; nothing here comes from user input.
/// </summary>
public static class RetentionConditions
{
    public static WhereCondition WithoutEmail() =>
        new WhereCondition().WhereEmpty("ContactEmail");

    public static WhereCondition WithEmail() =>
        new WhereCondition().WhereNotEmpty("ContactEmail");

    /// <summary>Created before <paramref name="cutoff"/> with no activity since.</summary>
    public static WhereCondition InactiveSince(DateTime cutoff) =>
        new WhereCondition()
            .WhereLessThan("ContactCreated", cutoff)
            .Where(new WhereCondition($"""
                NOT EXISTS (
                    SELECT 1 FROM OM_Activity RetentionActivity
                    WHERE RetentionActivity.ActivityContactID = OM_Contact.ContactID
                        AND RetentionActivity.ActivityCreated >= '{Literal(cutoff)}')
                """));

    /// <summary>The contact's email isn't a member's email.</summary>
    public static WhereCondition NotMember() =>
        new($"""
            NOT EXISTS (
                SELECT 1 FROM CMS_Member RetentionMember
                WHERE RetentionMember.MemberEmail = OM_Contact.ContactEmail)
            """);

    /// <summary>The contact has no active recipient list subscription (portal rule, <see cref="ActiveSubscriptionSql"/>).</summary>
    public static WhereCondition NotActiveSubscriber() =>
        new($"""
            NOT EXISTS (
                SELECT 1 FROM {ActiveSubscriptionSql.MEMBER_TABLE}
                INNER JOIN {ActiveSubscriptionSql.CONFIRMATION_TABLE}
                    ON {ActiveSubscriptionSql.CONFIRMATION_TABLE}.EmailSubscriptionConfirmationContactID = {ActiveSubscriptionSql.MEMBER_TABLE}.ContactGroupMemberRelatedID
                WHERE {ActiveSubscriptionSql.MEMBER_TABLE}.ContactGroupMemberRelatedID = OM_Contact.ContactID
                    AND {ActiveSubscriptionSql.Predicate()})
            """);

    public static string Literal(DateTime date) => date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
