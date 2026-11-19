using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Home;

public class HomePageHeroViewComponent(IContentRetriever contentRetriever) : ViewComponent
{
    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(HomePage page, HomePageHeroTemplateProperties properties)
    {
        var image = Maybe<ImageViewModel>.None;
        var imageGuids = properties.HeroImage.Select(i => i.Identifier).ToList();

        if (imageGuids.Count > 0)
        {
            var images = await contentRetriever.RetrieveContentByGuids<ImageContent>(imageGuids);
            image = images.TryFirst().Map(ImageViewModel.Create);
        }

        var portalPage = new PortalPage(page);

        return View(
            "~/Features/Home/HomePageHero.cshtml",
            new HomePageHeroViewModel(
                portalPage.Title,
                Maybe.From(portalPage.ShortDescription).MapNullOrWhiteSpaceAsNone(),
                image));
    }
}

public record HomePageHeroViewModel(string Title, Maybe<string> Description, Maybe<ImageViewModel> Image);
