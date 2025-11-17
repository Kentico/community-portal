using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;

public class PaginationViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IPagedViewModel model) => View("~/Components/ViewComponents/Pagination/Pagination.cshtml", model);
}

public interface IPagedViewModel
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<KeyValuePair<string, string?>> GetRouteData(int page);
    public string PageLinkPath(int pageNumber, string path) => QueryHelpers.AddQueryString(path, GetRouteData(pageNumber));
    public string PageLinkPath(int pageNumber, HttpRequest request) => QueryHelpers.AddQueryString(request.Path, GetRouteData(pageNumber));
}
