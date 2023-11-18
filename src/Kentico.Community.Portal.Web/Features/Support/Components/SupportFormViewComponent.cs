using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Support;

public class SupportFormViewComponent : ViewComponent
{
    public const string Identifier = "CommunityPortal.FormSupport";

    public IViewComponentResult Invoke(SupportFormViewModel? model = default)
    {
        model ??= new SupportFormViewModel();

        return View("~/Features/Support/Components/SupportForm.cshtml", model);
    }
}

public class SupportFormViewModel : ICaptchaClientResponse
{
    [DataType(DataType.Text)]
    [DisplayName("First name")]
    public string FirstName { get; set; } = "";

    [DataType(DataType.Text)]
    [DisplayName("Last name")]
    public string LastName { get; set; } = "";

    [EmailAddress(ErrorMessage = "Invalid email")]
    [Required]
    public string Email { get; set; } = "";

    [Required]
    public string Company { get; set; } = "";

    [Required]
    public string Issue { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    [DisplayName("What is the cause?")]
    public string Cause { get; set; } = "";

    [DisplayName("How have you already tried to solve this problem?")]
    public string AttemptedResolution { get; set; } = "";

    [Required]
    public string Version { get; set; } = "";

    [DisplayName("Deployment model")]
    [Required]
    public string DeploymentModel { get; set; } = "";

    [Required]
    [UrlValidationRule]
    public string WebsiteURL { get; set; } = "";

    public IFormFile? Attachment { set; get; }

    [Required(ErrorMessage = "Captcha is required")]
    [HiddenInput]
    public string CaptchaToken { get; set; } = "";

    [DisplayName("Consent Agreement")]
    public bool ConsentAgreement { get; set; } = false;

    public bool IsSuccess { get; set; } = false;
}
