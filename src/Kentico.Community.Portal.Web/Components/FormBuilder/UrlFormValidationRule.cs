using Kentico.Forms.Web.Mvc;

[assembly: RegisterFormBuilderValidationRule(
    identifier: "CommunityPortal.FormValidationRule.Url",
    ruleType: typeof(Kentico.Community.Portal.Web.Components.FormBuilder.UrlFormValidationRule),
    name: "URL",
    description: "Validates that the entered value is an absolute HTTPS URL.")]

namespace Kentico.Community.Portal.Web.Components.FormBuilder;

public class UrlFormValidationRuleProperties : FormValidationRuleProperties;

public class UrlFormValidationRule : SingleFieldFormValidationRule<UrlFormValidationRuleProperties, string>
{
    public const string IDENTIFIER = "CommunityPortal.FormValidationRule.Url";

    protected override Task<bool> Validate(string value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(true);
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(uri.Scheme is "https");
    }

    public override Task<string> GetDefaultErrorMessage() => Task.FromResult("Enter a valid HTTPS URL.");
}
