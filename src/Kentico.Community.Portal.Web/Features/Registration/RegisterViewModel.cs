using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Registration;

public class RegisterViewModel : ICaptchaClientResponse
{
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

    [DisplayName("Consent Agreement")]
    public bool ConsentAgreement { get; set; } = false;

    [Required(ErrorMessage = "Captcha is required")]
    [HiddenInput]
    public string CaptchaToken { get; set; } = "";
}
