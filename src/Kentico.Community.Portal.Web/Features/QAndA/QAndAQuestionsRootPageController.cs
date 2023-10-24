using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    contentTypeName: QAndAQuestionsRootPage.CONTENT_TYPE_NAME,
    controllerType: typeof(QAndAQuestionsRootPageController),
    ActionName = nameof(QAndAQuestionsRootPageController.Index)
)]

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndAQuestionsRootPageController : Controller
{
    public ActionResult Index() => RedirectPermanent("/q-and-a");
}
