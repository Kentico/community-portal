using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Infrastructure;

namespace Kentico.Community.Portal.Web.Features.Support;

[Route("[controller]/[action]")]
public class SupportFormController : Controller
{
    private readonly SupportFacade supportFacade;
    private readonly CaptchaValidator captchaValidator;

    public SupportFormController(SupportFacade supportFacade, CaptchaValidator captchaValidator)
    {
        this.supportFacade = supportFacade;
        this.captchaValidator = captchaValidator;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSupportForm([FromForm] SupportFormViewModel requestModel)
    {
        var captchaResponse = await captchaValidator.ValidateCaptcha(requestModel);

        if (!captchaResponse.IsSuccess)
        {
            ModelState.AddModelError(nameof(SupportFormViewModel.CaptchaToken), "Captcha is invalid.");
        }

        if (!ModelState.IsValid)
        {
            return ViewComponent(typeof(SupportFormViewComponent), requestModel);
        }

        await supportFacade.ProcessRequest(requestModel);

        return ViewComponent(typeof(SupportFormViewComponent), new SupportFormViewModel() { IsSuccess = true });
    }
}
