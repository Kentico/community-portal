using Kentico.Community.Portal.Web.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Kentico.Community.Portal.Web.Components.Widgets.FormSupport;

/// <summary>
/// View model for the form support widget.
/// </summary>
public class FormSupportWidgetViewModel : ICaptchaClientResponse
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email")]
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Company is required")]
    public string Company { get; set; }

    [Required(ErrorMessage = "Field is required")]
    public string Issue { get; set; }

    [Required(ErrorMessage = "Field is required")]
    public string Description { get; set; }

    public string Cause { get; set; }

    public string Fix { get; set; }

    [Required(ErrorMessage = "Version is required")]
    public string Version { get; set; }

    public string DeploymentModel { get; set; }

    [Required(ErrorMessage = "Website url is required")]
    public string Website { get; set; }
    public IFormFile Attachment { set; get; }

    [Required(ErrorMessage = "Captcha is required")]
    public string CaptchaToken { get; set; }
    public string SiteKey { get; set; }
}
