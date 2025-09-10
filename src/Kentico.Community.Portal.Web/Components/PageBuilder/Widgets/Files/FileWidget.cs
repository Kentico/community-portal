using CMS.ContentEngine;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Files;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Slugify;

[assembly: RegisterWidget(
    identifier: FileWidget.IDENTIFIER,
    viewComponentType: typeof(FileWidget),
    name: "File",
    propertiesType: typeof(FileWidgetProperties),
    Description = "Displays a link to a file in the page.",
    IconClass = KenticoIcons.FILE)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Files;

public class FileWidget(IContentRetriever contentRetriever, ISlugHelper slugHelper) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.FileWidget";
    public const string NAME = "File";
    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<FileWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var contentItemGUID = props.FileContents.Select(c => c.Identifier).TryFirst();

        return await Validate(props, contentItemGUID)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/Files/File.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<Result<FileWidgetViewModel, ComponentErrorViewModel>> Validate(FileWidgetProperties props, Maybe<Guid> contentItemGUID)
    {
        if (!contentItemGUID.TryGetValue(out var guid))
        {
            return Result.Failure<FileWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No content item has been selected."));
        }

        var item = await contentRetriever
            .RetrieveContentOfContentTypes<IContentItemFieldsSource>(
                [FileContent.CONTENT_TYPE_NAME, ImageContent.CONTENT_TYPE_NAME, VideoContent.CONTENT_TYPE_NAME],
                new RetrieveContentOfContentTypesParameters { IncludeSecuredItems = User.Identity?.IsAuthenticated ?? false },
                q => q.Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemGUID), guid)),
                new RetrievalCacheSettings($"{nameof(FileWidget)}|{guid}"))
            .TryFirst();

        if (!item.TryGetValue(out var i))
        {
            return Result.Failure<FileWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item could not be found."));
        }

        var asset = i switch
        {
            FileContent file => file.FileContentAsset,
            ImageContent image => image.ImageContentAsset,
            VideoContent video => video.VideoContentAsset,
            _ => null
        };

        if (asset is null || i is not IBasicItemFields basicItem)
        {
            return Result.Failure<FileWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item is not a media content item."));
        }

        return new FileWidgetViewModel(props, asset, basicItem, slugHelper);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 4)]
public class FileWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        [FileContent.CONTENT_TYPE_NAME, ImageContent.CONTENT_TYPE_NAME, VideoContent.CONTENT_TYPE_NAME],
        Label = "Selected file",
        MinimumItems = 1,
        MaximumItems = 1,
        ExplanationText = "Select a File Content item from the Content hub.",
        Order = 2)]
    public IEnumerable<ContentItemReference> FileContents { get; set; } = [];

    [TextInputComponent(
        Label = "Custom label",
        ExplanationText = "A custom label for the link to the file. If none is provided, the File Content Title value will be used.",
        Order = 3)]
    public string CustomLabel { get; set; } = "";

    [DropDownComponent(
        Label = "Design",
        ExplanationText = "The overall design of the link",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkDesigns>),
        Order = 5
    )]
    public string LinkDesign { get; set; } = nameof(LinkDesigns.Link);
    public LinkDesigns LinkDesignsParsed => EnumDropDownOptionsProvider<LinkDesigns>.Parse(LinkDesign, LinkDesigns.Link);

    [DropDownComponent(
        Label = "Alignment",
        ExplanationText = "The alignment of the link",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkAlignments>),
        Order = 6
    )]
    public string LinkAlignment { get; set; } = nameof(LinkAlignments.Left);
    public LinkAlignments LinkAlignmentsParsed => EnumDropDownOptionsProvider<LinkAlignments>.Parse(LinkAlignment, LinkAlignments.Left);

    [DropDownComponent(
        Label = "Padding",
        ExplanationText = "The vertical and horizontal padding of the link",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkPaddings>),
        Order = 7
    )]
    public string LinkPadding { get; set; } = nameof(LinkPaddings.Medium);
    public LinkPaddings LinkPaddingsParsed => EnumDropDownOptionsProvider<LinkPaddings>.Parse(LinkPadding, LinkPaddings.Medium);
}

public enum LinkAlignments { Left, Center, Right }
public enum LinkPaddings { None, Small, Medium, Large }
public enum LinkDesigns { Link, Button, Image }

public class FileWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = FileWidget.NAME;

    public LinkAlignments Alignment { get; }
    public LinkPaddings Padding { get; set; }
    public LinkDesigns Design { get; }
    public string Label { get; }
    public string ShortDescription { get; }
    public string AnchorSlug { get; }
    public ContentItemAsset Asset { get; }

    public FileWidgetViewModel(FileWidgetProperties props, ContentItemAsset asset, IBasicItemFields basicItem, ISlugHelper slugHelper)
    {
        Label = string.IsNullOrWhiteSpace(props.CustomLabel)
            ? basicItem.BasicItemTitle
            : props.CustomLabel;
        ShortDescription = basicItem.BasicItemShortDescription;
        Design = props.LinkDesignsParsed;
        Padding = props.LinkPaddingsParsed;
        Alignment = props.LinkAlignmentsParsed;
        AnchorSlug = slugHelper.GenerateSlug(Label);
        Asset = asset;
    }
}
