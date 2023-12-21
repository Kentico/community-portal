using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;

public class PaginationViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IPagedViewModel model) => View("~/Components/ViewComponents/Pagination/Pagination.cshtml", model);
}

public interface IPagedViewModel
{
    int Page { get; set; }
    int TotalPages { get; set; }
    Dictionary<string, string?> GetRouteData(int page);
    string PageLinkPath(int pageNumber, string path) => QueryHelpers.AddQueryString(path, GetRouteData(pageNumber));
    string PageLinkPath(int pageNumber, HttpRequest request) => QueryHelpers.AddQueryString(request.Path, GetRouteData(pageNumber));
}
