using CMS.Core;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormValidationRule(
    identifier: MemberAlternateEmailValidationRule.IDENTIFIER,
    ruleType: typeof(MemberAlternateEmailValidationRule),
    name: "Member alternate email",
    description: "Rejects an alternate email that is the member's account email or another member's account or verified alternate email")]
[assembly: RegisterFormValidationRule(
    identifier: MemberAccountEmailValidationRule.IDENTIFIER,
    ruleType: typeof(MemberAccountEmailValidationRule),
    name: "Member account email",
    description: "Rejects an account email that is another member's account or verified alternate email")]

namespace Kentico.Community.Portal.Admin.Features.Members;

/// <summary>
/// Checks if an email is used by another member. Implemented in the Web project
/// (MemberEmailVerificationService), so admin and self-service share one uniqueness rule.
/// </summary>
public interface IMemberEmailInUseChecker
{
    /// <summary>
    /// True when another member has <paramref name="email"/> as their account email or verified alternate email.
    /// </summary>
    Task<bool> IsEmailInUse(string email, int excludedMemberID);
}

/// <summary>
/// Server-side uniqueness check for the member Edit form alternate email field.
/// Email format is checked by the built-in Email rule.
/// </summary>
public class MemberAlternateEmailValidationRule(IMemberEmailInUseChecker emailInUseChecker)
    : ValidationRule<MemberAlternateEmailValidationRuleProperties, string>
{
    public const string IDENTIFIER = "Kentico.Community.Portal.ValidationRule.MemberAlternateEmail";
    public const string ACCOUNT_EMAIL_FIELD = nameof(MemberInfo.MemberEmail);

    public const string SAME_AS_ACCOUNT_EMAIL_MESSAGE = "The alternate email can't be the member's account email.";
    public const string IN_USE_MESSAGE = "Another member uses this email as their account email or verified alternate email.";

    private readonly IMemberEmailInUseChecker emailInUseChecker = emailInUseChecker;

    protected override string DefaultErrorMessage => IN_USE_MESSAGE;

    public override async Task<ValidationResult> Validate(string value, IFormFieldValueProvider formFieldValueProvider)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return await ValidationResult.SuccessResult();
        }

        string email = value.Trim();
        var member = (FormContext as InfoEditFormContext)?.EditedObject as MemberInfo;

        string accountEmail = formFieldValueProvider.TryGet<string>(ACCOUNT_EMAIL_FIELD, out string? submittedEmail)
            ? submittedEmail ?? ""
            : member?.MemberEmail ?? "";

        if (string.Equals(accountEmail.Trim(), email, StringComparison.OrdinalIgnoreCase))
        {
            return await ValidationResult.FailResult(SAME_AS_ACCOUNT_EMAIL_MESSAGE);
        }

        // Unchanged: don't block saving other fields over an existing value
        if (member is not null && string.Equals(member.GetStringValue("MemberAlternateEmail", "").Trim(), email, StringComparison.OrdinalIgnoreCase))
        {
            return await ValidationResult.SuccessResult();
        }

        return await (await emailInUseChecker.IsEmailInUse(email, member?.MemberID ?? 0)
            ? ValidationResult.FailResult(IN_USE_MESSAGE)
            : ValidationResult.SuccessResult());
    }
}

/// <summary>
/// Server-side check for the member Edit form account email field. Xperience already rejects another
/// member's account email; this also rejects another member's verified alternate email.
/// </summary>
public class MemberAccountEmailValidationRule(IMemberEmailInUseChecker emailInUseChecker)
    : ValidationRule<MemberAlternateEmailValidationRuleProperties, string>
{
    public const string IDENTIFIER = "Kentico.Community.Portal.ValidationRule.MemberAccountEmail";

    private readonly IMemberEmailInUseChecker emailInUseChecker = emailInUseChecker;

    protected override string DefaultErrorMessage => MemberAlternateEmailValidationRule.IN_USE_MESSAGE;

    public override async Task<ValidationResult> Validate(string value, IFormFieldValueProvider formFieldValueProvider)
    {
        var member = (FormContext as InfoEditFormContext)?.EditedObject as MemberInfo;

        // Empty: the built-in Required rule's job. Unchanged: don't block saving other fields.
        if (string.IsNullOrWhiteSpace(value)
            || (member is not null && string.Equals(member.MemberEmail?.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return await ValidationResult.SuccessResult();
        }

        return await (await emailInUseChecker.IsEmailInUse(value.Trim(), member?.MemberID ?? 0)
            ? ValidationResult.FailResult(MemberAlternateEmailValidationRule.IN_USE_MESSAGE)
            : ValidationResult.SuccessResult());
    }
}

public class MemberAlternateEmailValidationRuleProperties : ValidationRuleProperties
{
    public override string GetDescriptionText(ILocalizationService localizationService) =>
        "Must not be used by another member";
}
