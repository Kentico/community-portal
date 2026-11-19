using Kentico.Community.Portal.Admin.Features.DataProtection;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.DigitalMarketing;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(RightToBeForgottenExtender))]

namespace Kentico.Community.Portal.Admin.Features.DataProtection;

/// <summary>
/// Options of a Right to be forgotten request. Property names are the <see cref="ErasureOptionKeys"/>
/// the portal's eraser reads. All on by default: an erasure request usually means all of it.
/// </summary>
public class CommunityDataErasureDialogModel : IDataErasureDialogModel
{
    [CheckBoxComponent(
        Label = "Anonymize member",
        ExplanationText = "Archives the account, replaces the email and user name, renames to \"Former member\", clears profile fields, the avatar and external logins. Q&A content stays, attributed to \"Former member\".",
        Order = 0)]
    public bool AnonymizeMember { get; set; } = true;

    [CheckBoxComponent(
        Label = "Delete contacts",
        ExplanationText = "Deletes contacts with this email, with their activities and consent agreements.",
        Order = 1)]
    public bool DeleteContacts { get; set; } = true;

    [CheckBoxComponent(
        Label = "Delete short-lived form submissions",
        ExplanationText = "Deletes submissions of forms whose retention policy is Short-lived.",
        Order = 2)]
    public bool DeleteShortLivedFormData { get; set; } = true;

    [CheckBoxComponent(
        Label = "Remove email from kept form submissions",
        ExplanationText = "Replaces the email in submissions of History and unclassified forms. The rows and member IDs stay.",
        Order = 3)]
    public bool ScrubFormEmails { get; set; } = true;

    [CheckBoxComponent(
        Label = "Remove subscriptions",
        ExplanationText = "Removes recipient list memberships and Q&A discussion subscriptions and notification settings.",
        Order = 4)]
    public bool RemoveSubscriptions { get; set; } = true;

    [CheckBoxComponent(
        Label = "Unlink email statistics",
        ExplanationText = "Deletes stored copies of emails sent to this address and unlinks their open/click hits. The hits stay for aggregate statistics.",
        Order = 5)]
    public bool UnlinkEmailStatistics { get; set; } = true;

    public Task<ValidationResult> Validate() =>
        AnonymizeMember || DeleteContacts || DeleteShortLivedFormData || ScrubFormEmails
            || RemoveSubscriptions || UnlinkEmailStatistics
            ? ValidationResult.SuccessResult()
            : ValidationResult.FailResult("Select at least one option.");
}

public class RightToBeForgottenExtender : PageExtender<RightToBeForgotten>
{
    public override Task ConfigurePage()
    {
        Page.PageConfiguration.DataErasureDialogModel = new CommunityDataErasureDialogModel();

        return base.ConfigurePage();
    }
}
