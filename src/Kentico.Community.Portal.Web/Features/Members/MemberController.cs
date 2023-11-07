using Kentico.Community.Portal.Web.Features.Blog.Models;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Membership;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Members;

[Route("[controller]")]
public class MemberController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly SearchService search;

    public MemberController(
        IMediator mediator,
        WebPageMetaService metaService,
        SearchService search)
    {
        this.mediator = mediator;
        this.metaService = metaService;
        this.search = search;
    }

    [HttpGet("{memberID:int}")]
    public async Task<IActionResult> MemberDetail(int memberID)
    {
        var memberInfo = await mediator.Send(new MemberByIDQuery(memberID));

        if (memberInfo is null)
        {
            return NotFound();
        }

        var member = memberInfo.AsCommunityMember();

        metaService.SetMeta(new($"Member Profile - {member.UserName}", $"Learn about {member.UserName} and their contributions to the Kentico Community"));

        var blogResult = search.SearchBlog(new BlogSearchRequest("date", 50)
        {
            AuthorMemberID = member.Id
        });

        var qandaResult = search.SearchQAndA(new QAndASearchRequest("date", 50)
        {
            AuthorMemberID = member.Id,
        });

        var model = new MemberDetailViewModel
        {
            Member = member,
            BlogPostLinks = blogResult.Hits.Select(h => new BlogPostLink(h.Url, h.Title, h.PublishedDate, h.Taxonomy)).ToList(),
            QuestionsAsked = qandaResult.Hits.Select(h => new Link(h.Url, h.Title, h.DateCreated)).ToList()
        };

        return View("~/Features/Members/MemberDetail.cshtml", model);
    }
}

public class MemberDetailViewModel
{
    public CommunityMember Member { get; set; }
    public IReadOnlyList<Link> QuestionsAsked { get; set; } = new List<Link>();
    public IReadOnlyList<BlogPostLink> BlogPostLinks { get; set; } = new List<BlogPostLink>();
}

public record BlogPostLink(
    string Path,
    string Title,
    DateTime PublishedDate,
    string Taxonomy);
public record Link(string Path, string Label, DateTime Date);
