using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.SEO.StructuredData;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public class IntegrationsListViewComponent(
    IMediator mediator,
    ITaxonomyRetriever taxonomyRetriever,
    LinkGenerator linkGenerator,
    JsonLdCollector jsonLd) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private readonly LinkGenerator linkGenerator = linkGenerator;
    private readonly JsonLdCollector jsonLd = jsonLd;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var resp = await mediator.Send(new IntegrationContentsQuery());
        var tagIdentifiers = resp.Items.Select(i => i.Content.IntegrationContentIntegrationType.Select(t => t.Identifier).FirstOrDefault());
        var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.IntegrationType.TaxonomyName, PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var items = resp.Items
            .Select(i =>
            {
                var (content, author) = i;
                return new IntegrationItemViewModel(content, taxonomy, author, linkGenerator);
            })
            .ToList();

        // Built from the items this view renders, so every value is visible on the page.
        // Search and type filtering only hide entries (x-show), so the full server-rendered list is marked up.
        // Added before the layout renders the head - keep this view component out of output caching.
        jsonLd.Add(ToJsonLd(items));

        return View("~/Features/Integrations/Components/IntegrationsList.cshtml", new IntegrationsListViewModel(items, taxonomy.Tags));
    }

    private ItemListJsonLd ToJsonLd(IReadOnlyList<IntegrationItemViewModel> items)
    {
        string pageURL = Request.CanonicalURL();

        return new([.. items.Select((item, index) => new SoftwareListItemJsonLd(
            Position: index + 1,
            Item: new SoftwareApplicationJsonLd(
                Name: item.Title,
                URL: item.RepositoryURL
                    .Or(item.LibraryURL)
                    .GetValueOrDefault($"{pageURL}#{item.Anchor}"),
                Description: string.IsNullOrWhiteSpace(item.ShortDescription) ? null : item.ShortDescription,
                ApplicationCategory: string.IsNullOrWhiteSpace(item.Type.Title) ? null : item.Type.Title,
                Author: item.AuthorLink
                    .Map(a => new PersonJsonLd(a.Label, a.URL.RelativePathToAbsoluteURL(Request)))
                    .GetValueOrDefault())))]);
    }
}

public record IntegrationsListViewModel(IReadOnlyList<IntegrationItemViewModel> Items, IEnumerable<Tag> IntegrationTypes);

public class IntegrationItemViewModel
{
    public string Title { get; }
    /// <summary>
    /// Element id for <c>#anchor</c> links. From the content item code name, so it survives reordering.
    /// </summary>
    public string Anchor { get; }
    public Maybe<ImageViewModel> Logo { get; }
    public string ShortDescription { get; }
    public Maybe<string> RepositoryURL { get; }
    public Maybe<string> LibraryURL { get; }
    public IntegrationTypeViewModel Type { get; }
    public Maybe<IntegrationAuthorLink> AuthorLink { get; }
    public Maybe<IntegrationBusiness> Business { get; }

    public IntegrationItemViewModel(IntegrationContent content, TaxonomyData taxonomy, Maybe<CommunityMember> member, LinkGenerator linkGenerator)
    {
        Title = content.BasicItemTitle;
        Anchor = AnchorFor(content);
        Logo = content.ToImageViewModel();
        ShortDescription = content.BasicItemShortDescription;
        RepositoryURL = Maybe.From(content.IntegrationContentRepositoryLinkURL)
            .MapNullOrWhiteSpaceAsNone()
            .Map(NormalizeRepositoryURL);
        LibraryURL = Maybe.From(content.IntegrationContentLibraryLinkURL).MapNullOrWhiteSpaceAsNone();

        AuthorLink = GetMemberLink();
        Business = Maybe.From(content.IntegrationContentAuthorName)
            .MapNullOrWhiteSpaceAsNone()
            .Map(name => new IntegrationBusiness(name, Maybe.From(content.IntegrationContentAuthorLinkURL).MapNullOrWhiteSpaceAsNone()));

        Type = content.IntegrationContentIntegrationType
            .TryFirst()
            .Bind(t => taxonomy.Tags.TryFirst(tag => tag.Identifier == t.Identifier))
            .Map(t => new IntegrationTypeViewModel(t.Name, t.Title))
            .GetValueOrDefault(IntegrationTypeViewModel.Default);

        Maybe<IntegrationAuthorLink> GetMemberLink()
        {
            if (!member.TryGetValue(out var m))
            {
                return Maybe<IntegrationAuthorLink>.None;
            }

            string memberURL = linkGenerator.GetPathByAction(nameof(MemberController.MemberDetail), "Member", new { memberID = m.Id }) ?? "";

            return new IntegrationAuthorLink(m.DisplayName, memberURL);
        }
    }

    /// <summary>
    /// One canonical form per repository URL: trimmed, no trailing slash
    /// </summary>
    public static string NormalizeRepositoryURL(string url) => url.Trim().TrimEnd('/');

    /// <summary>
    /// Element id of an integration on the hub. From the content item code name, so it survives reordering.
    /// </summary>
    public static string AnchorFor(IntegrationContent content) =>
        $"extension-{content.SystemFields.ContentItemName.ToLowerInvariant()}";
}

public record IntegrationAuthorLink(string Label, string URL);
public record IntegrationBusiness(string Label, Maybe<string> URL);
public record IntegrationTypeViewModel(string CodeName, string Title)
{
    public static IntegrationTypeViewModel Default { get; } = new("", "");
};
