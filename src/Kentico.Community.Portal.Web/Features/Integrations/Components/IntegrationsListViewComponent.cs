using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Rendering;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public class IntegrationsListViewComponent(
    IMediator mediator,
    AssetItemService itemService,
    ITaxonomyRetriever taxonomyRetriever) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly AssetItemService itemService = itemService;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var resp = await mediator.Send(new IntegrationContentsQuery());

        var items = new List<IntegrationItemViewModel>();

        var tagIdentifiers = resp.Items.Select(i => i.IntegrationContentIntegrationType.Select(t => t.Identifier).FirstOrDefault());
        var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.IntegrationType.TaxonomyName, PortalWebSiteChannel.DEFAULT_LANGUAGE);

        foreach (var c in resp.Items)
        {
            var logo = await itemService.RetrieveMediaFileImage(c.IntegrationContentLogoMediaFile.FirstOrDefault());

            items.Add(new IntegrationItemViewModel
            {
                Title = c.IntegrationContentTitle,
                Logo = logo,
                ShortDescription = c.IntegrationContentShortDescription,
                RepositoryURL = string.IsNullOrWhiteSpace(c.IntegrationContentRepositoryLinkURL) ? null : c.IntegrationContentRepositoryLinkURL,
                LibraryURL = string.IsNullOrWhiteSpace(c.IntegrationContentLibraryLinkURL) ? null : c.IntegrationContentLibraryLinkURL,
                AuthorName = c.AuthorNameNormalized,
                Type = c.IntegrationContentIntegrationType
                    .TryFirst()
                    .Bind(t => taxonomy.Tags.TryFirst(tag => tag.Identifier == t.Identifier))
                    .Map(t => new IntegrationTypeViewModel(t.Name, t.Title))
                    .GetValueOrDefault(new(c.IntegrationContentType.ToString(), c.IntegrationTypeDisplayName)),
                AuthorURL = string.IsNullOrWhiteSpace(c.IntegrationContentAuthorLinkURL) ? null : c.IntegrationContentAuthorLinkURL
            });
        }

        return View("~/Features/Integrations/Components/IntegrationsList.cshtml", new IntegrationsListViewModel { Items = items, IntegrationTypes = taxonomy.Tags });
    }
}

public class IntegrationsListViewModel
{
    public IReadOnlyList<IntegrationItemViewModel> Items { get; set; } = [];
    public IEnumerable<Tag> IntegrationTypes { get; set; } = [];
}

public class IntegrationItemViewModel
{
    public string Title { get; set; } = "";
    public ImageAssetViewModel? Logo { get; set; }
    public string ShortDescription { get; set; } = "";
    public string? RepositoryURL { get; set; }
    public string? LibraryURL { get; set; }
    public IntegrationTypeViewModel Type { get; set; } = new("", "");
    public string AuthorName { get; set; } = "";
    public string? AuthorURL { get; set; }
}

public record IntegrationTypeViewModel(string CodeName, string Title);
