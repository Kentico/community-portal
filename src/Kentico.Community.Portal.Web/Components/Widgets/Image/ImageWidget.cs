using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Components.Widgets.Image;
using CMS.ContentEngine;
using MediatR;
using Kentico.Community.Portal.Web.Rendering;

[assembly: RegisterWidget(
    identifier: ImageWidget.IDENTIFIER,
    viewComponentType: typeof(ImageWidget),
    name: ImageWidget.NAME,
    propertiesType: typeof(ImageWidgetProperties),
    Description = "A simple image Widget",
    IconClass = "icon-camera")]

namespace Kentico.Community.Portal.Web.Components.Widgets.Image;

public class ImageWidget(IMediator mediator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.ImageWidget";
    public const string NAME = "Image";

    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ImageWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var contentItemGUID = props.SelectedImages.Select(i => i.Identifier).FirstOrDefault();

        var image = await mediator.Send(new ImageContentByGUIDQuery(contentItemGUID));

        return Validate(props, image)
            .Match(
                vm => View("~/Components/Widgets/Image/Image.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private Result<ImageWidgetViewModel, ComponentErrorViewModel> Validate(ImageWidgetProperties props, Maybe<ImageContent> image)
    {
        if (props.SelectedImages.FirstOrDefault() is null)
        {
            return Result.Failure<ImageWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No image has been selected."));
        }

        if (!image.TryGetValue(out var img) || img.ImageContentAsset is null)
        {
            return Result.Failure<ImageWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or image file no longer exists."));
        }

        return new ImageWidgetViewModel(ImageViewModel.Create(img));
    }
}

public class ImageWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        ImageContent.CONTENT_TYPE_NAME,
        Label = "Selected image",
        MinimumItems = 1,
        MaximumItems = 1,
        DefaultViewMode = Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> SelectedImages { get; set; } = [];
}

public class ImageWidgetViewModel(ImageViewModel image) : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = ImageWidget.NAME;

    public ImageViewModel Image { get; } = image;
}
