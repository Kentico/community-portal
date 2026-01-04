using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Image;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: ImageWidget.IDENTIFIER,
    viewComponentType: typeof(ImageWidget),
    name: ImageWidget.NAME,
    propertiesType: typeof(ImageWidgetProperties),
    Description = "A simple image Widget",
    IconClass = KenticoIcons.CAMERA,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Image;

public class ImageWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.ImageWidget";
    public const string NAME = "Image";

    private readonly IContentRetriever contentRetriever = contentRetriever;


    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ImageWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var image = await contentRetriever
            .RetrieveContentByGuids<ImageContent>(props.SelectedImages.Select(i => i.Identifier))
            .TryFirst();

        return Validate(props, image)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/Image/Image.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<ImageWidgetViewModel, ComponentErrorViewModel> Validate(ImageWidgetProperties props, Maybe<ImageContent> image)
    {
        if (props.SelectedImages.FirstOrDefault() is null)
        {
            return Result.Failure<ImageWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No image has been selected."));
        }

        if (!image.TryGetValue(out var img) || img.ImageContentAsset is null)
        {
            return Result.Failure<ImageWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or image file no longer exists."));
        }

        return new ImageWidgetViewModel(ImageAssetViewModel.Create(img), props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class ImageWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        ImageContent.CONTENT_TYPE_NAME,
        Label = "Selected image",
        MinimumItems = 1,
        MaximumItems = 1,
        DefaultViewMode = Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> SelectedImages { get; set; } = [];

    [DropDownComponent(
        Label = "Image Alignment",
        ExplanationText = "The alignment of the image",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ImageAlignments>),
        Order = 4
    )]
    public string Alignment { get; set; } = nameof(ImageAlignments.Center);
    public ImageAlignments AlignmentParsed => EnumDropDownOptionsProvider<ImageAlignments>.Parse(Alignment, ImageAlignments.Center);

    [DropDownComponent(
        Label = "Image Size",
        ExplanationText = "The size of the image",
        Tooltip = "Select a size",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ImageSizes>),
        Order = 5
    )]
    public string Size { get; set; } = nameof(ImageSizes.Full_Width);
    public ImageSizes SizeParsed => EnumDropDownOptionsProvider<ImageSizes>.Parse(Size, ImageSizes.Full_Width);

    [CheckBoxComponent(
        Label = "Show description as caption?",
        ExplanationText = "If true, a caption will appear below the image, populated by the image's description field.",
        Order = 6
    )]
    public bool ShowDescriptionAsCaption { get; set; } = false;

    [CheckBoxComponent(
        Label = "Link image to full size file?",
        ExplanationText = "If true, the image will be linked to a full resolution version of the image.",
        Order = 7)]
    public bool LinkToFullsizeImage { get; set; } = true;
}

public class ImageWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = ImageWidget.NAME;

    public ImageAssetViewModel Image { get; }
    public bool ShowDescriptionAsCaption { get; }
    public bool LinkToFullsizeImage { get; }
    public ImageAlignments Alignment { get; }
    public ImageSizes Size { get; }

    public ImageWidgetViewModel(ImageAssetViewModel image, ImageWidgetProperties props)
    {
        Image = image;
        ShowDescriptionAsCaption = props.ShowDescriptionAsCaption;
        LinkToFullsizeImage = props.LinkToFullsizeImage;
        Alignment = props.AlignmentParsed;
        Size = props.SizeParsed;
    }
}

public enum ImageAlignments
{
    [Description("Left")]
    Left,
    [Description("Center")]
    Center,
    [Description("Right")]
    Right
}

public enum ImageSizes
{
    [Description("Small")]
    Small,
    [Description("Medium")]
    Medium,
    [Description("Large")]
    Large,
    [Description("Full width")]
    Full_Width,
}
