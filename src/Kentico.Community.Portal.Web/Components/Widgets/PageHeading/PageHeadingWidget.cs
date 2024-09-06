using Kentico.Community.Portal.Web.Components.Widgets.PageHeading;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: PageHeadingWidget.IDENTIFIER,
    viewComponentType: typeof(PageHeadingWidget),
    name: "Page Heading",
    propertiesType: typeof(PageHeadingWidgetProperties),
    Description = "Text to appear at the top of a Page",
    IconClass = "icon-t",
    AllowCache = true
)]

namespace Kentico.Community.Portal.Web.Components.Widgets.PageHeading;

public class PageHeadingWidget : ViewComponent
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Web.Widgets.PageHeading";

    public IViewComponentResult Invoke(ComponentViewModel<PageHeadingWidgetProperties> vm)
    {
        var model = new PageHeadingWidgetViewModel(vm.Properties);

        return View("~/Components/Widgets/PageHeading/PageHeading.cshtml", model);
    }
}

public class PageHeadingWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Title",
        Order = 1
    )]
    public string Title { get; set; } = "";

    [TextAreaComponent(
        Label = "Message",
        Order = 2
    )]
    public string? Message { get; set; } = null;
}

public class PageHeadingWidgetViewModel(PageHeadingWidgetProperties props)
{
    public string Title { get; set; } = props.Title;
    public string? Message { get; set; } = props.Message;
}
