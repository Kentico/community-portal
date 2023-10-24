using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Kentico.Community.Portal.Web.Resources;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;

public class NavigationViewComponent : ViewComponent
{
    private readonly IHtmlLocalizer<SharedResources> localizer;

    public NavigationViewComponent(
        IHtmlLocalizer<SharedResources> localizer) => this.localizer = localizer;

    public IViewComponentResult Invoke()
    {
        var model = GetMenuItems();

        return View("~/Components/ViewComponents/Navigation/Navigation.cshtml", model);
    }

    private HeaderViewModel GetMenuItems()
    {
        var menuItems = new List<MenuItemViewModel>()
        {
            new()
            {
                Caption = localizer["Home"].Value,
                Url = "/",
                Level = 1,
            },
            new()
            {
                Caption = localizer["Community"].Value,
                Url = "/community",
                Level = 1,
            },
            new()
            {
                Caption = localizer["Resource Hub"].Value,
                Url = "/resource-hub",
                Level = 1,
            },
            new()
            {
                Caption = localizer["Blog"].Value,
                Url = "/blog",
                Level = 1,
            },
            new()
            {
                Caption = localizer["Q&A"].Value,
                Url = "/q-and-a",
                Level = 1,
            },
            new()
            {
                Caption = localizer["Support"].Value,
                Url = "/support",
                Level = 1,
            }
        };

        var model = new HeaderViewModel()
        {
            FirstLevel = menuItems.Select(m =>
            {
                m.IsActive = Request.Path.StartsWithSegments(m.Url);
                return m;
            })
            .ToList(),
        };

        return model;
    }
}

public class HeaderViewModel
{
    public List<MenuItemViewModel> FirstLevel { get; set; }
}

public class MenuItemViewModel : LinkViewModel
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public int Level { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

public class LinkViewModel
{
    public string Caption { get; set; }
    public string Url { get; set; }
    public bool IsSelected { get; set; }
}
