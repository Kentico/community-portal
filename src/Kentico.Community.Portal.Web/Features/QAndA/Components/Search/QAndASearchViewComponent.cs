using Kentico.Community.Portal.Web.Components.ViewComponents.Pagination;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndASearchViewComponent : ViewComponent
{
    private readonly SearchService searchService;

    public QAndASearchViewComponent(SearchService searchService) => this.searchService = searchService;

    public IViewComponentResult Invoke()
    {
        var request = new QAndASearchRequest(HttpContext.Request);

        var searchResult = searchService.SearchQAndA(request);

        var vm = new QAndASearchViewModel();

        var hits = searchResult.Hits.ToList();
        var ids = hits.Select(x => x.ID).ToList();

        var viewModels = hits.Select(QAndAPostViewModel.GetModel).ToList();

        var viewModelsSorted = new List<QAndAPostViewModel>();

        foreach (var post in hits)
        {
            var model = viewModels.Single(x => x.ID == post.ID);
            model.LinkPath = post.Url;

            viewModelsSorted.Add(model);
        }

        vm.Questions = viewModelsSorted;
        vm.Page = request.PageNumber;
        vm.SortBy = request.SortBy;
        vm.Query = request.SearchText;
        vm.TotalPages = searchResult.TotalPages;

        return View("~/Features/QAndA/Components/Search/QAndASearch.cshtml", vm);
    }
}

public class QAndASearchViewModel : IPagedViewModel
{
    public IReadOnlyList<QAndAPostViewModel> Questions { get; set; } = new List<QAndAPostViewModel>();

    public string Query { get; set; }
    [HiddenInput]
    public int Page { get; set; }
    public string SortBy { get; set; }
    public int TotalPages { get; set; }

    public Dictionary<string, string> GetRouteData(int page) =>
        new()
        {
            { "query", Query },
            { "page", page.ToString() },
            { "sortBy", SortBy }
        };
}

public class QAndAPostViewModel
{
    public int ID { get; set; }
    public string Title { get; set; } = "";
    public string LinkPath { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public int AnswersCount { get; set; }
    public bool IsAnswered { get; set; }
    public QAndAPostAuthorViewModel Author { get; set; } = new();

    public static QAndAPostViewModel GetModel(QAndASearchResult result) => new()
    {
        Title = result.Title,
        DateCreated = result.DateCreated,
        AnswersCount = result.AnswerCount,
        IsAnswered = result.IsAnswered,
        Author = new()
        {
            FullName = result.AuthorFullName,
            MemberID = result.AuthorMemberID,
            Username = result.AuthorUsername
        },
        LinkPath = result.Url,
        ID = result.ID
    };
}

public class QAndAPostAuthorViewModel
{
    public int MemberID { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
}

