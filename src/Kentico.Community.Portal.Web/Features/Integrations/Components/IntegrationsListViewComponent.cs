using Kentico.Community.Portal.Web.Rendering;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using static Kentico.Community.Portal.Core.Content.IntegrationContent;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public class IntegrationsListViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly AssetItemService itemService;

    public IntegrationsListViewComponent(IMediator mediator, AssetItemService itemService)
    {
        this.mediator = mediator;
        this.itemService = itemService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var resp = await mediator.Send(new IntegrationContentsQuery());

        var items = new List<IntegrationItemViewModel>();

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
                Type = c.TypeParsed,
                AuthorURL = string.IsNullOrWhiteSpace(c.IntegrationContentAuthorLinkURL) ? null : c.IntegrationContentAuthorLinkURL
            });
        }

        return View("~/Features/Integrations/Components/IntegrationsList.cshtml", new IntegrationsListViewModel { Items = items });
    }
}

public class IntegrationsListViewModel
{
    public IReadOnlyList<IntegrationItemViewModel> Items { get; set; } = new List<IntegrationItemViewModel>();
}

public class IntegrationItemViewModel
{
    public string Title { get; set; } = "";
    public ImageAssetViewModel? Logo { get; set; }
    public string ShortDescription { get; set; } = "";
    public string? RepositoryURL { get; set; }
    public string? LibraryURL { get; set; }
    public IntegrationType Type { get; set; }
    public string AuthorName { get; set; } = "";
    public string? AuthorURL { get; set; }
}
