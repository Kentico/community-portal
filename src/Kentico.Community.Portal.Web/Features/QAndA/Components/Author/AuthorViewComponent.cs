using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class AuthorViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(QAndAAuthorViewModel author) =>
        View("~/Features/QAndA/Components/Author/Author.cshtml", author);
}
