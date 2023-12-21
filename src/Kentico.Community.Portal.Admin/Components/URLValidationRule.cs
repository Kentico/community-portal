using CMS.Core;
using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormValidationRule(
    identifier: URLValidationRule.IDENTIFIER,
    ruleType: typeof(URLValidationRule),
    name: "URL",
    description: "Requires a value is a valid URL")]

namespace Kentico.Community.Portal.Admin;

[ValidationRuleAttribute(typeof(URLValidationRuleAttribute))]
public class URLValidationRule : ValidationRule<URLValidationRuleProperties, URLValidationRuleClientProperties, string>
{
    public const string IDENTIFIER = "Kentico.Community.Portal.ValidationRule.URL";

    public override string ClientRuleName => "@kentico-community/portal-web-admin/URLValidationRule";

    protected override string DefaultErrorMessage => "The given value is not a valid URL";

    protected override Task ConfigureClientProperties(URLValidationRuleClientProperties clientProperties)
    {
        clientProperties.RequireHTTPS = Properties.RequireHTTPS;

        return base.ConfigureClientProperties(clientProperties);
    }

    public override Task<ValidationResult> Validate(string value, IFormFieldValueProvider formFieldValueProvider)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.SuccessResult();
        }

        // Validation logic
        bool result = Uri.TryCreate(value, UriKind.Absolute, out var uriResult)
            && uriResult.Scheme == Uri.UriSchemeHttps;

        if (result)
        {
            return ValidationResult.SuccessResult();
        }

        return ValidationResult.FailResult($"{DefaultErrorMessage}: {(result ? uriResult?.ToString() : value)}");
    }
}

public class URLValidationRuleProperties : ValidationRuleProperties
{
    public override string GetDescriptionText(ILocalizationService localizationService) => "Must be a valid URL";

    [CheckBoxComponent(
        Label = "Require https",
        ExplanationText = "If true the URL must begin with https. True by default."
    )]
    public bool RequireHTTPS { get; set; } = true;

    public override string ErrorMessage { get => base.ErrorMessage; set => base.ErrorMessage = value; }
}

public class URLValidationRuleClientProperties : ValidationRuleClientProperties
{
    public bool RequireHTTPS { get; set; }
}

public class URLValidationRuleAttribute : ValidationRuleAttribute
{
    public bool RequireHTTPS { get; set; } = true;
}
