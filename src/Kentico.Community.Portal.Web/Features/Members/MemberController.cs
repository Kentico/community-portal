using CMS.Helpers;
using Kentico.Community.Portal.Web.Features.Blog.Search;
using Kentico.Community.Portal.Web.Features.Community;
using Kentico.Community.Portal.Web.Features.Integrations;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Features.QAndA.Search;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Kentico.Community.Portal.Web.Features.Members;

[Route("[controller]")]
public class MemberController(
    IMediator mediator,
    WebPageMetaService metaService,
    BlogSearchService blogSearchService,
    QAndASearchService qAndASearchService,
    AvatarImageService avatarImageService,
    MemberBadgeService memberBadgeService) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly AvatarImageService avatarImageService = avatarImageService;
    private readonly WebPageMetaService metaService = metaService;
    private readonly BlogSearchService search = blogSearchService;
    private readonly QAndASearchService qAndASearchService = qAndASearchService;
    private readonly MemberBadgeService memberBadgeService = memberBadgeService;

    [HttpGet("{memberID:int}")]
    public async Task<IActionResult> MemberDetail(int memberID)
    {
        var memberInfo = await mediator.Send(new MemberByIDQuery(memberID));
        if (memberInfo is null)
        {
            return NotFound();
        }

        var member = memberInfo.AsCommunityMember();

        metaService.SetMeta(new WebPageMeta(
            $"Community member profile - {member.UserName}",
            $"Learn about {member.UserName} and their contributions to the Kentico Community"));

        var blogResult = search.SearchBlog(new BlogSearchRequest("publishdate", 50)
        {
            AuthorMemberID = member.Id
        });

        var qandaResult = qAndASearchService.SearchQAndA(new QAndASearchRequest("publishdate", 50)
        {
            AuthorMemberID = member.Id,
        });

        var badges = await memberBadgeService.GetAllBadgesFor(member.Id);
        var integrationsResp = await mediator.Send(new IntegrationContentsByMemberIDQuery(member.Id));
        var contributionsResp = await mediator.Send(new LinkContentsByMemberIDQuery(member.Id));

        var model = new MemberDetailViewModel(member)
        {
            Page = new PortalPage("Community member profile", $"Learn about {member.UserName} and their contributions to the Kentico Community"),
            BlogPostLinks = [.. blogResult.Hits.Select(h => new BlogPostLink(h.Url, h.Title, h.PublishedDate, h.BlogType))],
            QuestionsAsked = [.. qandaResult.Hits.Select(h => new Link(h.Url, h.Title, h.PublishedDate))],
            MemberBadges = badges,
            Contributions = [.. contributionsResp.Items.Select(c => new Link(c.LinkContentPathOrURL, c.BasicItemTitle, c.LinkContentPublishedDate))],
            Integrations = [.. integrationsResp.Items.Select(i => new Link(i.IntegrationContentRepositoryLinkURL, i.BasicItemTitle, i.IntegrationContentPublishedDate))],
        };

        return View("~/Features/Members/MemberDetail.cshtml", model);
    }

    [HttpGet]
    [Route("avatar/{memberID:int}")]
    public async Task<ActionResult> GetAvatarImage(int memberID)
    {
        var file = await avatarImageService.GetAvatarImage(memberID);

        long lastModified = file.LastWriteTime.Ticks;
        long length = file.Length;
        string eTagNew = $"\"{lastModified}-{length}\"";

        Response.Headers.CacheControl = "public";

        /*
         * There's an issue where Xperience middleware clears the cache control header
         * sent from this response, preventing the etag/last modified from being used
         */
        if (Request.Headers.TryGetValue("If-None-Match", out var value))
        {
            string? eTagOld = value.First();
            if (string.Equals(eTagOld, eTagNew))
            {
                return new StatusCodeResult(304);
            }
        }

        return File(
            file.OpenRead(),
            MimeTypeHelper.GetMimetype(file.Extension),
            file.LastWriteTime,
            entityTag: new EntityTagHeaderValue(eTagNew));
    }
}

public class MemberDetailViewModel(CommunityMember member)
{
    public required PortalPage Page { get; init; }
    public CommunityMember Member { get; init; } = member;
    public IReadOnlyList<Link> QuestionsAsked { get; init; } = [];
    public IReadOnlyList<BlogPostLink> BlogPostLinks { get; init; } = [];
    public IReadOnlyList<Link> Contributions { get; init; } = [];
    public IReadOnlyList<Link> Integrations { get; init; } = [];
    public MemberAvatarViewModel MemberAvatar { get; set; } = new(member);
    public IReadOnlyList<MemberBadgeViewModel> MemberBadges = [];
}

public class MemberAvatarViewModel
{
    public int ID { get; }
    public string Username { get; } = "";
    public string FullName { get; } = "";
    public string FormattedName =>
        string.IsNullOrWhiteSpace(FullName)
            ? Username
            : $"{FullName} ({Username})";

    public MemberAvatarViewModel(CommunityMember member)
    {
        ID = member.Id;
        Username = member.UserName!;
        FullName = member.FullName;
    }

    public MemberAvatarViewModel() { }
}

public record BlogPostLink(
    string Path,
    string Title,
    DateTime PublishedDate,
    string Taxonomy);
public record Link(string Path, string Label, DateTime Date);
