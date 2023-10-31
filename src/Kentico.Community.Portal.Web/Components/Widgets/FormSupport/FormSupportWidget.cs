using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Kentico.Community.Portal.Web.Components.Widgets.FormSupport;
using Kentico.Community.Portal.Web.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

[assembly: RegisterWidget(
    identifier: FormSupportWidgetViewComponent.Identifier,
    viewComponentType: typeof(FormSupportWidgetViewComponent),
    name: "Form support",
    propertiesType: typeof(FormSupportWidgetProperties),
    Description = "Displays a support form.",
    IconClass = "icon-form")]

namespace Kentico.Community.Portal.Web.Components.Widgets.FormSupport;

public class FormSupportWidgetViewComponent : ViewComponent
{
    public const string Identifier = "CommunityPortal.FormSupport";

    public ViewViewComponentResult Invoke()
    {
        var model = new FormSupportWidgetViewModel();

        return View("~/Components/Widgets/FormSupport/FormSupport.cshtml", model);
    }
}

public class FormSupportWidgetProperties : IWidgetProperties
{
}

public class FormSupportWidgetViewModel : ICaptchaClientResponse
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

    [DataType(DataType.Url)]
    [Required]
    [Url]
    public string WebsiteURL { get; set; } = "";

    public IFormFile? Attachment { set; get; }

    [Required(ErrorMessage = "Captcha is required")]
    [HiddenInput]
    public string CaptchaToken { get; set; } = "";

    [DisplayName("Consent Agreement")]
    public bool ConsentAgreement { get; set; } = false;

    public bool IsSuccess { get; set; } = false;
}
