using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Features.Support;

namespace Kentico.Community.Portal.Web.Components.Widgets.FormSupport;

[ApiController]
[Route("api/[controller]")]
public class FormsController : ControllerBase
{
    private readonly SupportFacade supportFacade;
    private readonly CaptchaValidator captchaValidator;

    public FormsController(SupportFacade supportFacade, CaptchaValidator captchaValidator)
    {
        this.supportFacade = supportFacade;
        this.captchaValidator = captchaValidator;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] FormSupportWidgetViewModel requestModel)
    {
        var captchaResponse = await captchaValidator.ValidateCaptcha(requestModel);

        if (!captchaResponse.IsSuccess)
        {
            return BadRequest(captchaResponse);
        }

        await supportFacade.ProcessRequest(requestModel);

        return Ok();
    }
}
