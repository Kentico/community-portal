using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

public class RegisterViewModel : ICaptchaClientResponse
{
    public const string TERMS_REQUIRED_MESSAGE = "Please accept the Terms of Use to create an account.";

    [DataType(DataType.Text)]
    [Required(ErrorMessage = "Please enter your username")]
    [DisplayName("User name")]
    [RegularExpression("^[a-zA-Z0-9_\\-\\.]+$", ErrorMessage = "Please enter a valid username")]
    [MaxLength(100, ErrorMessage = "Maximum allowed length of the input text is {1}")]
    public string UserName { get; set; } = "";


    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "Please enter your email")]
    [DisplayName("Email")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [MaxLength(100, ErrorMessage = "Maximum allowed length of the input text is {1}")]
    public string Email { get; set; } = "";


    [DataType(DataType.Password)]
    [DisplayName("Password")]
    [Required(ErrorMessage = "Please enter your password")]
    [MaxLength(100, ErrorMessage = "Maximum allowed length of the input text is {1}")]
    public string Password { get; set; } = "";


    [DataType(DataType.Password)]
    [DisplayName("Confirm your password")]
    [Required(ErrorMessage = "Please confirm your password")]
    [MaxLength(100, ErrorMessage = "Maximum allowed length of the input text is {1}")]
    [Compare("Password", ErrorMessage = "Password does not match the confirmation password")]
    public string PasswordConfirmation { get; set; } = "";

    /// <summary>
    /// Required. The browser enforces it with the <c>required</c> attribute on the checkbox.
    /// </summary>
    [DisplayName("Terms of Use")]
    [Range(typeof(bool), "true", "true", ErrorMessage = TERMS_REQUIRED_MESSAGE)]
    public bool AcceptTerms { get; set; } = false;

    /// <summary>
    /// Optional. Subscribes the member when they confirm their email.
    /// </summary>
    [DisplayName("Newsletter")]
    public bool SubscribeToNewsletter { get; set; } = false;

    [Required(ErrorMessage = "Captcha is required")]
    [HiddenInput]
    public string CaptchaToken { get; set; } = "";

    /// <summary>
    /// The widget's editable text. Set by the widget and the controller, never bound from the request.
    /// Null: the view uses the defaults.
    /// </summary>
    [BindNever]
    [ValidateNever]
    public RegisterFormContent? Content { get; set; }
}
