using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public class IntegrationsListViewComponent(
    IMediator mediator,
    ITaxonomyRetriever taxonomyRetriever,
    LinkGenerator linkGenerator,
    IJSEncoder jSEncoder) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private readonly LinkGenerator linkGenerator = linkGenerator;
    private readonly IJSEncoder jSEncoder = jSEncoder;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var resp = await mediator.Send(new IntegrationContentsQuery());
        var tagIdentifiers = resp.Items.Select(i => i.Content.IntegrationContentIntegrationType.Select(t => t.Identifier).FirstOrDefault());
        var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.IntegrationType.TaxonomyName, PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var items = resp.Items
            .Select(i =>
            {
                var (content, author) = i;
                return new IntegrationItemViewModel(content, taxonomy, author, linkGenerator, jSEncoder);
            })
            .ToList();

        return View("~/Features/Integrations/Components/IntegrationsList.cshtml", new IntegrationsListViewModel(items, taxonomy.Tags));
    }
}

public record IntegrationsListViewModel(IReadOnlyList<IntegrationItemViewModel> Items, IEnumerable<Tag> IntegrationTypes);

public class IntegrationItemViewModel
{
    public string Title { get; }
    public Maybe<ImageViewModel> Logo { get; }
    public string ShortDescription { get; }
    public Maybe<string> RepositoryURL { get; }
    public Maybe<string> LibraryURL { get; }
    public IntegrationTypeViewModel Type { get; }
    public string AuthorName { get; }
    public Maybe<string> AuthorURL { get; }
    public IHtmlContent MetadataJSON { get; }

    public IntegrationItemViewModel(IntegrationContent content, TaxonomyData taxonomy, Maybe<CommunityMember> author, LinkGenerator linkGenerator, IJSEncoder jsEncoder)
    {
        Title = content.ListableItemTitle;
        Logo = content.ToImageViewModel();
        ShortDescription = content.ListableItemShortDescription;
        RepositoryURL = Maybe.From(content.IntegrationContentRepositoryLinkURL).MapNullOrWhiteSpaceAsNone();
        LibraryURL = Maybe.From(content.IntegrationContentLibraryLinkURL).MapNullOrWhiteSpaceAsNone();

        AuthorName = author
            .Map(a => a.FullName)
            .GetValueOrDefault(content.IntegrationContentAuthorName);

        Type = content.IntegrationContentIntegrationType
            .TryFirst()
            .Bind(t => taxonomy.Tags.TryFirst(tag => tag.Identifier == t.Identifier))
            .Map(t => new IntegrationTypeViewModel(t.Name, t.Title))
            .GetValueOrDefault(IntegrationTypeViewModel.Default);

        AuthorURL = author
            .Map(a => linkGenerator.GetPathByAction(nameof(MemberController.MemberDetail), "Member", new { memberID = a.Id }))
            .Match(_ => _, () => Maybe.From(content.IntegrationContentAuthorLinkURL).MapNullOrWhiteSpaceAsNone());

        MetadataJSON = jsEncoder.EncodeToJson(new
        {
            title = Title.ToLowerInvariant(),
            description = ShortDescription.ToLowerInvariant(),
            type = Type.CodeName
        });
    }
}

public record IntegrationTypeViewModel(string CodeName, string Title)
{
    public static IntegrationTypeViewModel Default { get; } = new("", "");
};
