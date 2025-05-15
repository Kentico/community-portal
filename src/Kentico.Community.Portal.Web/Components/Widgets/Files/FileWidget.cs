using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.Widgets.Files;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Slugify;

[assembly: RegisterWidget(
    identifier: FileWidget.IDENTIFIER,
    viewComponentType: typeof(FileWidget),
    name: "File",
    propertiesType: typeof(FileWidgetProperties),
    Description = "Displays a link to a file in the page.",
    IconClass = KenticoIcons.FILE)]

namespace Kentico.Community.Portal.Web.Components.Widgets.Files;

public class FileWidget(IMediator mediator, ISlugHelper slugHelper) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.FileWidget";
    public const string NAME = "File";
    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<FileWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var file = await mediator.Send(new MediaItemContentByGUIDQuery(props.FileContents.FirstOrDefault()?.Identifier ?? default));

        return Validate(props, file)
            .Match(
                vm => View("~/Components/Widgets/Files/File.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private Result<FileWidgetViewModel, ComponentErrorViewModel> Validate(FileWidgetProperties props, Maybe<IMediaItem> mediaItem)
    {
        if (!mediaItem.TryGetValue(out var media))
        {
            return Result.Failure<FileWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No media item has been selected."));
        }

        // populate this switch statement with all the content types that implement IMediaItem
        var asset = media switch
        {
            FileContent file => file.FileContentAsset,
            ImageContent image => image.ImageContentAsset,
            VideoContent video => video.VideoContentAsset,
            _ => null
        };

        if (asset is null)
        {
            return Result.Failure<FileWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item is not a media ."));
        }

        return new FileWidgetViewModel(props, asset, media, slugHelper);
    }
}

public record MediaItemContentByGUIDQuery(Guid ContentItemGUID) : IQuery<Maybe<IMediaItem>>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemGUID.ToString();
}

public class MediaItemByGUIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<MediaItemContentByGUIDQuery, Maybe<IMediaItem>>(tools)
{
    public override async Task<Maybe<IMediaItem>> Handle(MediaItemContentByGUIDQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfReusableSchema(IMediaItem.REUSABLE_FIELD_SCHEMA_NAME))
            .Parameters(q => q.Where(w => w.WhereContentItem(request.ContentItemGUID)));

        return await Executor
            .GetMappedResult<IMediaItem>(b, DefaultQueryOptions, cancellationToken)
            .TryFirst();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MediaItemContentByGUIDQuery query, Maybe<IMediaItem> result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemGUID);
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 4)]
public class FileWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(typeof(MediaItemSchemasFilter),
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

    public FileWidgetViewModel(FileWidgetProperties props, ContentItemAsset asset, IMediaItem mediaItem, ISlugHelper slugHelper)
    {
        Label = string.IsNullOrWhiteSpace(props.CustomLabel)
            ? mediaItem.MediaItemTitle
            : props.CustomLabel;
        ShortDescription = mediaItem.MediaItemShortDescription;
        Design = props.LinkDesignsParsed;
        Padding = props.LinkPaddingsParsed;
        Alignment = props.LinkAlignmentsParsed;
        AnchorSlug = slugHelper.GenerateSlug(Label);
        Asset = asset;
    }
}

public class MediaItemSchemasFilter : IReusableFieldSchemasFilter
{
    public IEnumerable<string> AllowedSchemaNames => [IMediaItem.REUSABLE_FIELD_SCHEMA_NAME];
}
