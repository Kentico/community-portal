using Kentico.Builder.Web.Mvc;
using Kentico.Community.Portal.Web.Components.PageBuilder.Sections;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.PageBuilder;

public abstract class BasePageBuilderComponent : ViewComponent
{
    protected bool IsValid => ModelState.ErrorCount > 0;

    protected void AddModelError(IComponentProperties properties, string error)
    {
        if (properties is BaseWidgetProperties widgetProperties)
        {
            ModelState.AddModelError(widgetProperties.ID.ToString(), error);
        }
        else if (properties is BaseSectionProperties sectionProperties)
        {
            ModelState.AddModelError(sectionProperties.ID.ToString(), error);
        }
    }
}
