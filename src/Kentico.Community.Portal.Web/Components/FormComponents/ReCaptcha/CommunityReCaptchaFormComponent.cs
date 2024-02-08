using System.ComponentModel.DataAnnotations;
using System.Globalization;

using CMS.Core;
using CMS.FormEngine;
using CMS.Helpers;
using CMS.Helpers.Internal;
using Kentico.Community.Portal.Web.Components.FormComponents.ReCaptcha;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Forms.Web.Mvc;
using Microsoft.Extensions.Options;

[assembly: RegisterFormComponent(
    identifier: CommunityReCaptchaFormComponent.IDENTIFIER,
    formComponentType: typeof(CommunityReCaptchaFormComponent),
    name: "Community ReCaptcha",
    Description = "{$kentico.formbuilder.component.recaptcha.description$}",
    IconClass = "icon-recaptcha",
    ViewName = "~/Components/FormComponents/ReCaptcha/CommunityReCaptcha.cshtml",
    IsMappableToContactFields = false)]

namespace Kentico.Community.Portal.Web.Components.FormComponents.ReCaptcha;

/// <summary>
/// Represents a reCAPTCHA form component.
/// </summary>
public class CommunityReCaptchaFormComponent : FormComponent<CommunityReCaptchaFormComponentProperties, string>
{
    /// <summary>
    /// Represents the <see cref="CommunityReCaptchaFormComponent"/> identifier.
    /// </summary>
    public const string IDENTIFIER = "CommunityPortal.Components.Recaptcha-FormComponent";

    private const string RESPONSE_KEY = "g-recaptcha-response";

    private static readonly Lazy<HashSet<string>> mFullLanguageCodes = new(() =>
    {
        string[] codes = ["zh-HK", "zh-CN", "zh-TW", "en-GB", "fr-CA", "de-AT", "de-CH", "pt-BR", "pt-PT"];

        return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    });

    private readonly IAppSettingsService appSettingsService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILocalizationService localizationService;
    private readonly ReCaptchaSettings captchaOptions;
    private string mLanguage = "";
    private bool? mSkipRecaptcha;


    /// <summary>
    /// Holds nothing and is here just because it is required.
    /// </summary>
    public string Value
    {
        get;
        set;
    } = "";

    /// <summary>
    /// Optional. Forces the reCAPTCHA to render in a specific language.
    /// Auto-detects the user's language if unspecified.
    /// Currently supported values are listed at https://developers.google.com/recaptcha/docs/language.
    /// </summary>
    public string Language
    {
        get
        {
            if (string.IsNullOrEmpty(mLanguage))
            {
                var currentCulture = CultureInfo.CurrentCulture;

                mLanguage = mFullLanguageCodes.Value.Contains(currentCulture.Name) ? currentCulture.Name : currentCulture.TwoLetterISOLanguageName;
            }

            return mLanguage;
        }
        set => mLanguage = value;
    }


    /// <summary>
    /// Determines whether the component is configured and allowed to be displayed.
    /// </summary>
    public bool IsConfigured => AreKeysConfigured && !SkipRecaptcha;


    /// <summary>
    /// Indicates whether to skip the reCAPTCHA validation.
    /// Useful for testing platform. Can be set using RecaptchaSkipValidation in AppSettings.
    /// </summary>
    private bool SkipRecaptcha
    {
        get
        {
            if (!mSkipRecaptcha.HasValue)
            {
                mSkipRecaptcha = ValidationHelper.GetBoolean(appSettingsService["RecaptchaSkipValidation"], false);
            }

            return mSkipRecaptcha.Value;
        }
    }


    /// <summary>
    /// Indicates whether both required keys are configured in the Settings application.
    /// </summary>
    private bool AreKeysConfigured => !string.IsNullOrEmpty(captchaOptions.SiteKey) && !string.IsNullOrEmpty(captchaOptions.SecretKey);


    /// <summary>
    /// Label "for" cannot be used for this component. 
    /// </summary>
    public override string LabelForPropertyName => null!;


    /// <summary>
    /// Initializes a new instance of <see cref="RecaptchaComponent"/>
    /// </summary>
    public CommunityReCaptchaFormComponent(
        IAppSettingsService appSettingsService,
        IHttpContextAccessor httpContextAccessor,
        ILocalizationService localizationService,
        IOptions<ReCaptchaSettings> captchaOptions)
    {
        this.appSettingsService = appSettingsService;
        this.httpContextAccessor = httpContextAccessor;
        this.localizationService = localizationService;
        this.captchaOptions = captchaOptions.Value;
    }


    /// <inheritdoc/>
    public override void LoadProperties(FormComponentProperties properties)
    {
        base.LoadProperties(properties);

        Properties.Label = string.Empty;
    }


    /// <summary>
    /// Returns empty string since the <see cref="Value"/> does not hold anything.
    /// </summary>
    /// <returns>Returns the value of the form component.</returns>
    public override string GetValue() => string.Empty;


    /// <summary>
    /// Does nothing since the <see cref="Value"/> does not need to hold anything.
    /// </summary>
    /// <param name="value">Value to be set.</param>
    public override void SetValue(string value)
    {
        // the Value does not need to hold anything
    }


    /// <summary>
    /// Performs validation of the reCAPTCHA component.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection that holds failed-validation information.</returns>
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();
        errors.AddRange(base.Validate(validationContext));

        bool isRenderedInAdminUI = VirtualContext.IsInitialized;

        if (!IsConfigured || isRenderedInAdminUI)
        {
            return errors;
        }

        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return new List<ValidationResult> { new("Cannot validate captcha without outside of an active HTTP request") };
        }

        var validationResult = ValidateResponse(httpContext);
        if (validationResult != null)
        {
            if (!string.IsNullOrEmpty(validationResult.ErrorMessage))
            {
                errors.Add(new ValidationResult(localizationService.GetString(validationResult.ErrorMessage)));
            }
            else
            {
                if (validationResult.Score < captchaOptions.ScoreThredhold)
                {
                    errors.Add(new ValidationResult(localizationService.GetString("recaptcha.error.scorenotmet")));
                }
                if (!ActionIsValid(httpContext, validationResult))
                {
                    errors.Add(new ValidationResult(localizationService.GetString("recaptcha.error.actionmismatch")));
                }
            }
        }
        else
        {
            errors.Add(new ValidationResult(localizationService.GetString("recaptcha.error.serverunavailable")));
        }

        return errors;
    }


    private RecaptchaResponse ValidateResponse(HttpContext httpContext)
    {
        var validator = new RecaptchaValidator
        {
            PrivateKey = captchaOptions.SecretKey,
            RemoteIP = RequestContext.UserHostAddress,
            Response = httpContext.Request.Form.TryGetValue(RESPONSE_KEY, out var value) ? value[0] : string.Empty
        };

        return validator.Validate().Result;
    }


    private static bool ActionIsValid(HttpContext httpContext, RecaptchaResponse validationResult)
    {
        if (!httpContext.Request.Form.TryGetValue("g-recaptcha-action", out var action))
        {
            return false;
        }

        if (action[0] is not string actionValue)
        {
            return false;
        }

        return actionValue.Equals(validationResult.Action, StringComparison.OrdinalIgnoreCase);
    }
}

public class CommunityReCaptchaFormComponentProperties : FormComponentProperties<string>
{
    public CommunityReCaptchaFormComponentProperties() : base("text", 1, -1)
    {
    }

    public override bool Required { get; set; }
    public override string DefaultValue { get; set; } = "";
    public override bool SmartField { get; set; }
    public override string Tooltip { get; set; } = "";
}

