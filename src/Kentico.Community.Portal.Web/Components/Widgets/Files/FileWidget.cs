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

        var file = await mediator.Send(new FileContentByGUIDQuery(props.FileContents.FirstOrDefault()?.Identifier ?? default));

        return Validate(props, file)
            .Match(
                vm => View("~/Components/Widgets/Files/File.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private Result<FileWidgetViewModel, ComponentErrorViewModel> Validate(FileWidgetProperties props, Maybe<FileContent> fileContent)
    {
        if (!fileContent.TryGetValue(out var file))
        {
            return Result.Failure<FileWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No file has been set for this widget."));
        }

        if (file.FileContentAsset is null)
        {
            return Result.Failure<FileWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected file has no asset."));
        }

        return new FileWidgetViewModel(props, file, slugHelper);
    }
}

public record FileContentByGUIDQuery(Guid ContentItemGUID) : IQuery<Maybe<FileContent>>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemGUID.ToString();
}

public class FileContentByGUIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<FileContentByGUIDQuery, Maybe<FileContent>>(tools)
{
    public override async Task<Maybe<FileContent>> Handle(FileContentByGUIDQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                FileContent.CONTENT_TYPE_NAME,
                q => q.ForContentItem(request.ContentItemGUID));

        return await Executor.GetMappedResult<FileContent>(b, DefaultQueryOptions, cancellationToken)
            .TryFirst();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(FileContentByGUIDQuery query, Maybe<FileContent> result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemGUID);
}


public class FileWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(FileContent.CONTENT_TYPE_NAME,
        Label = "Selected file",
        MinimumItems = 1,
        MaximumItems = 1,
        ExplanationText = "Select a File Content item from the Content hub.",
        Order = 1)]
    public IEnumerable<ContentItemReference> FileContents { get; set; } = [];

    [TextInputComponent(
        Label = "Custom label",
        ExplanationText = "A custom label for the link to the file. If none is provided, the File Content Title value will be used.",
        Order = 2)]
    public string CustomLabel { get; set; } = "";

    [DropDownComponent(
        Label = "Link alignment",
        ExplanationText = "Sets the alignment of the link",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkAlignments>),
        Order = 3
    )]
    public string LinkAlignment { get; set; } = nameof(LinkAlignments.Left);
    public LinkAlignments LinkAlignmentsParsed => EnumDropDownOptionsProvider<LinkAlignments>.Parse(LinkAlignment, LinkAlignments.Left);

    [DropDownComponent(
        Label = "Link padding",
        ExplanationText = "Sets the vertical and horizontal padding of the link",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkPaddings>),
        Order = 4
    )]
    public string LinkPadding { get; set; } = nameof(LinkPaddings.Medium);
    public LinkPaddings LinkPaddingsParsed => EnumDropDownOptionsProvider<LinkPaddings>.Parse(LinkPadding, LinkPaddings.Medium);

    [DropDownComponent(
        Label = "Link design",
        ExplanationText = "The design of the link to the file",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkDesigns>),
        Order = 5
    )]
    public string LinkDesign { get; set; } = nameof(LinkDesigns.Link);
    public LinkDesigns LinkDesignsParsed => EnumDropDownOptionsProvider<LinkDesigns>.Parse(LinkDesign, LinkDesigns.Link);
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
    public string AnchorSlug { get; }
    public FileContent File { get; }

    public FileWidgetViewModel(FileWidgetProperties props, FileContent file, ISlugHelper slugHelper)
    {
        Label = string.IsNullOrWhiteSpace(props.CustomLabel)
            ? file.MediaItemTitle
            : props.CustomLabel;
        Design = props.LinkDesignsParsed;
        Padding = props.LinkPaddingsParsed;
        Alignment = props.LinkAlignmentsParsed;
        AnchorSlug = slugHelper.GenerateSlug(Label);
        File = file;
    }
}
