using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Community;

public class CommunityLinksListViewComponent(
    IMediator mediator,
    LinkGenerator linkGenerator,
    IJSEncoder jSEncoder) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly LinkGenerator linkGenerator = linkGenerator;
    private readonly IJSEncoder jSEncoder = jSEncoder;

    public async Task<IViewComponentResult> InvokeAsync(int linkPublishDelayDays)
    {
        var resp = await mediator.Send(new LinkContentsCommunityQuery(linkPublishDelayDays));

        var items = resp
            .Select(i =>
            {
                var (content, member) = i;
                return new CommunityLinkItemViewModel(content, member, linkGenerator, jSEncoder);
            })
            .ToList();

        // Get unique members for filtering
        var members = resp
            .Where(i => i.Member.TryGetValue(out _))
            .Select(i => i.Member.GetValueOrDefault()!)
            .DistinctBy(m => m.Id)
            .OrderBy(m => m.DisplayName)
            .ToList();

        return View("~/Features/Community/Components/CommunityLinksList.cshtml", new CommunityLinksListViewModel(items, members));
    }
}

public record CommunityLinksListViewModel(IReadOnlyList<CommunityLinkItemViewModel> Items, IReadOnlyList<CommunityMember> Members);

public class CommunityLinkItemViewModel
{
    public string Title { get; }
    public string ShortDescription { get; }
    public string SearchIndex { get; }
    public string URL { get; }
    public DateTime PublishedDate { get; }
    public Maybe<CommunityLinkAuthor> Author { get; }
    public IHtmlContent MetadataJSON { get; }

    public CommunityLinkItemViewModel(LinkContent content, Maybe<CommunityMember> member, LinkGenerator linkGenerator, IJSEncoder jsEncoder)
    {
        Title = content.BasicItemTitle;
        ShortDescription = content.BasicItemShortDescription;
        SearchIndex = $"{Title} {ShortDescription}".ToLowerInvariant();
        URL = content.LinkContentPathOrURL;
        PublishedDate = content.LinkContentPublishedDate;

        Author = GetAuthorLink();

        MetadataJSON = jsEncoder.EncodeToJson(new
        {
            search = SearchIndex,
            publishedDate = PublishedDate.ToString("yyyy-MM-dd"),
            memberId = Author.Map(a => a.MemberId).GetValueOrDefault(0)
        });

        Maybe<CommunityLinkAuthor> GetAuthorLink()
        {
            if (!member.TryGetValue(out var m))
            {
                return Maybe<CommunityLinkAuthor>.None;
            }

            string memberURL = linkGenerator.GetPathByAction(nameof(MemberController.MemberDetail), "Member", new { memberID = m.Id }) ?? "";

            return new CommunityLinkAuthor(m.DisplayName, memberURL, m.Id);
        }
    }
}

public record CommunityLinkAuthor(string Name, string URL, int MemberId);
