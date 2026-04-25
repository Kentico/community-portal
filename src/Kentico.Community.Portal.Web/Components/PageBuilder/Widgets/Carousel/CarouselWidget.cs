using CMS.ContentEngine;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Carousel;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Image;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Testimonial;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ComponentIcons;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CarouselWidget.IDENTIFIER,
    viewComponentType: typeof(CarouselWidget),
    name: CarouselWidget.NAME,
    propertiesType: typeof(CarouselWidgetProperties),
    Description = CarouselWidget.DESCRIPTION,
    IconClass = KenticoIcons.CAMERA,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Carousel;

public class CarouselWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Widgets.Carousel";
    public const string NAME = "Carousel";
    public const string DESCRIPTION = "Displays up to 6 slides from Image and Testimonial content.";

    private const string VIEW_PATH = "~/Components/PageBuilder/Widgets/Carousel/Carousel.cshtml";
    private const string ERROR_NO_SELECTION = "Select between 1 and 6 Image or Testimonial items.";
    private const string ERROR_NO_VALID_ITEMS = "None of the selected items are currently available.";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<CarouselWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var selectedItems = props.Slides?.Take(6).ToArray() ?? [];

        if (selectedItems.Length == 0)
        {
            return View("~/Components/ComponentError.cshtml", new ComponentErrorViewModel(NAME, ComponentType.Widget, ERROR_NO_SELECTION));
        }

        var selectedGuids = selectedItems.Select(i => i.Identifier).Distinct().ToArray();

        var imageLookup = (await contentRetriever.RetrieveContentByGuids<ImageContent>(selectedGuids, new RetrieveContentParameters { LinkedItemsMaxLevel = 1 }))
            .ToDictionary(x => x.SystemFields.ContentItemGUID);

        var testimonialLookup = (await contentRetriever.RetrieveContentByGuids<TestimonialContent>(selectedGuids, new RetrieveContentParameters { LinkedItemsMaxLevel = 1 }))
            .ToDictionary(x => x.SystemFields.ContentItemGUID);

        var slides = new List<CarouselSlideViewModel>();

        foreach (var item in selectedItems)
        {
            if (testimonialLookup.ContainsKey(item.Identifier))
            {
                slides.Add(CarouselSlideViewModel.ForTestimonial(item.Identifier));
                continue;
            }

            if (imageLookup.ContainsKey(item.Identifier))
            {
                slides.Add(CarouselSlideViewModel.ForImage(item.Identifier));
            }
        }

        if (slides.Count == 0)
        {
            return View("~/Components/ComponentError.cshtml", new ComponentErrorViewModel(NAME, ComponentType.Widget, ERROR_NO_VALID_ITEMS));
        }

        return View(VIEW_PATH, new CarouselWidgetViewModel(props, slides));
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class CarouselWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        [TestimonialContent.CONTENT_TYPE_NAME, ImageContent.CONTENT_TYPE_NAME],
        Label = "Slides",
        ExplanationText = "Select between 1 and 6 Image and/or Testimonial items.",
        MinimumItems = 1,
        MaximumItems = 6,
        DefaultViewMode = ContentItemSelectorViewMode.Auto,
        Order = 1)]
    public IEnumerable<ContentItemReference> Slides { get; set; } = [];

    [TextInputComponent(
        Label = "Accessible label",
        ExplanationText = "Screen reader label for this carousel region.",
        Order = 2)]
    public string AriaLabel { get; set; } = "Featured content carousel";
}

public class CarouselWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => CarouselWidget.NAME;

    public string AriaLabel { get; }
    public IReadOnlyList<CarouselSlideViewModel> Slides { get; }

    public CarouselWidgetViewModel(CarouselWidgetProperties props, IReadOnlyList<CarouselSlideViewModel> slides)
    {
        AriaLabel = string.IsNullOrWhiteSpace(props.AriaLabel)
            ? "Featured content carousel"
            : props.AriaLabel;
        Slides = slides;
    }
}

public enum CarouselSlideTypes
{
    Image,
    Testimonial,
}

public class CarouselSlideViewModel
{
    public CarouselSlideTypes SlideType { get; }
    public string WidgetIdentifier { get; }
    public IWidgetProperties WidgetProperties { get; }

    private CarouselSlideViewModel(CarouselSlideTypes slideType, string widgetIdentifier, IWidgetProperties widgetProperties)
    {
        SlideType = slideType;
        WidgetIdentifier = widgetIdentifier;
        WidgetProperties = widgetProperties;
    }

    public static CarouselSlideViewModel ForImage(Guid contentItemGuid) =>
        new(
            CarouselSlideTypes.Image,
            ImageWidget.IDENTIFIER,
            new ImageWidgetProperties
            {
                SelectedImages = [new ContentItemReference { Identifier = contentItemGuid }],
                Alignment = nameof(ImageAlignments.Center),
                Size = nameof(ImageSizes.Full_Width),
                Theme = nameof(ImageThemes.Shadow),
                ShowDescriptionAsCaption = false,
                LinkToFullsizeImage = true,
            });

    public static CarouselSlideViewModel ForTestimonial(Guid contentItemGuid) =>
        new(
            CarouselSlideTypes.Testimonial,
            TestimonialWidget.IDENTIFIER,
            new TestimonialWidgetProperties
            {
                Testimonial = [new ContentItemReference { Identifier = contentItemGuid }],
                Layout = nameof(TestimonialLayouts.Featured),
                Theme = nameof(TestimonialThemes.Neutral),
                ShowTitle = true,
                ShowPhoto = true,
                ShowEmployment = true,
            });
}
