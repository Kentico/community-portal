using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.Blog.Components;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Members;

[Route("[controller]")]
public class MemberController(
    IMediator mediator,
    WebPageMetaService metaService,
    BlogSearchService blogSearchService,
    QAndASearchService qAndASearchService) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly WebPageMetaService metaService = metaService;
    private readonly BlogSearchService search = blogSearchService;
    private readonly QAndASearchService qAndASearchService = qAndASearchService;

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

        var qandaResult = qAndASearchService.SearchQAndA(new QAndASearchRequest("date", 50)
        {
            AuthorMemberID = member.Id,
        });

        var model = new MemberDetailViewModel(member)
        {
            BlogPostLinks = blogResult.Hits.Select(h => new BlogPostLink(h.Url, h.Title, h.PublishedDate, h.Taxonomy)).ToList(),
            QuestionsAsked = qandaResult.Hits.Select(h => new Link(h.Url, h.Title, h.PublishedDate)).ToList()
        };

        return View("~/Features/Members/MemberDetail.cshtml", model);
    }
}

public class MemberDetailViewModel(CommunityMember member)
{
    public CommunityMember Member { get; init; } = member;
    public IReadOnlyList<Link> QuestionsAsked { get; init; } = [];
    public IReadOnlyList<BlogPostLink> BlogPostLinks { get; init; } = [];
}

public record BlogPostLink(
    string Path,
    string Title,
    DateTime PublishedDate,
    string Taxonomy);
public record Link(string Path, string Label, DateTime Date);
