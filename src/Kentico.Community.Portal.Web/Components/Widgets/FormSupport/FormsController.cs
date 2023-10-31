using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Features.Support;

namespace Kentico.Community.Portal.Web.Components.Widgets.FormSupport;

[Route("[controller]/[action]")]
public class FormsController : Controller
{
    private readonly SupportFacade supportFacade;
    private readonly CaptchaValidator captchaValidator;

    public FormsController(SupportFacade supportFacade, CaptchaValidator captchaValidator)
    {
        this.supportFacade = supportFacade;
        this.captchaValidator = captchaValidator;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSupportForm([FromForm] FormSupportWidgetViewModel requestModel)
    {
        var captchaResponse = await captchaValidator.ValidateCaptcha(requestModel);

        if (!captchaResponse.IsSuccess)
        {
            ModelState.AddModelError(nameof(FormSupportWidgetViewModel.CaptchaToken), "Captcha is invalid.");
        }

        if (!ModelState.IsValid)
        {
            return PartialView("~/Components/Widgets/FormSupport/FormSupport.cshtml", requestModel);
        }

        await supportFacade.ProcessRequest(requestModel);

        return PartialView("~/Components/Widgets/FormSupport/FormSupport.cshtml", new FormSupportWidgetViewModel { IsSuccess = true });
    }
}
