using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

/// <summary>
/// Ensures that the editor.md library assets, used by the Q&A functionality, are rendered into the page
/// </summary>
public class EditorMdAssetsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() =>
        View("~/Features/QAndA/Components/Form/EditorMdAssets.cshtml");
}
