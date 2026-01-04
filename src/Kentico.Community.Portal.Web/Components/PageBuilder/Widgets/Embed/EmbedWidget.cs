using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Embed;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: EmbedWidget.IDENTIFIER,
    name: EmbedWidget.NAME,
    viewComponentType: typeof(EmbedWidget),
    propertiesType: typeof(EmbedWidgetProperties),
    Description = "Adds an HTML embed to the web page from an Embed Content item.",
    IconClass = KenticoIcons.XML_TAG,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Embed;

public class EmbedWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.EmbedWidget";
    public const string NAME = "Embed";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<EmbedWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var embed = await contentRetriever
            .RetrieveContentByGuids<EmbedContent>(props.SelectedEmbeds.Select(i => i.Identifier))
            .TryFirst();

        return Validate(props, embed)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/Embed/Embed.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<EmbedWidgetViewModel, ComponentErrorViewModel> Validate(EmbedWidgetProperties props, Maybe<EmbedContent> embed)
    {
        if (props.SelectedEmbeds.FirstOrDefault() is null)
        {
            return Result.Failure<EmbedWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No embed has been selected."));
        }

        if (!embed.TryGetValue(out var emb))
        {
            return Result.Failure<EmbedWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item no longer exists."));
        }

        if (string.IsNullOrWhiteSpace(emb.EmbedContentHTML))
        {
            return Result.Failure<EmbedWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected embed has no embed code."));
        }

        return new EmbedWidgetViewModel(props, emb);
    }
}

public class EmbedWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        EmbedContent.CONTENT_TYPE_NAME,
        Label = "Selected embed",
        MinimumItems = 1,
        MaximumItems = 1,
        Order = 1)]
    public IEnumerable<ContentItemReference> SelectedEmbeds { get; set; } = [];

    [DropDownComponent(
        Label = "Width",
        ExplanationText = "The width of the embed",
        Tooltip = "Select a width",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ComponentWidths>),
        Order = 2
    )]
    public string ComponentWidth { get; set; } = nameof(ComponentWidths.Full);
    public ComponentWidths ComponentWidthParsed => EnumDropDownOptionsProvider<ComponentWidths>.Parse(ComponentWidth, ComponentWidths.Full);

    [VisibleIfEqualTo(nameof(ComponentWidth), nameof(ComponentWidths.Natural))]
    [DropDownComponent(
        Label = "Alignment",
        ExplanationText = "The alignment of the embed",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HorizontalAlignments>),
        Order = 3
    )]
    public string HorizontalAlignment { get; set; } = nameof(HorizontalAlignments.Left);
    public HorizontalAlignments HorizontalAlignmentParsed => EnumDropDownOptionsProvider<HorizontalAlignments>.Parse(HorizontalAlignment, HorizontalAlignments.Left);
}

public enum ComponentWidths
{
    [Description("Natural")]
    Natural,
    [Description("Full")]
    Full
}

public enum HorizontalAlignments
{
    [Description("Left")]
    Left,
    [Description("Center")]
    Center,
    [Description("Right")]
    Right
}

public enum TagType
{
    Video,
    Social,
    Other
}

public class EmbedWidgetViewModel
{
    public HtmlString HTML { get; }
    public HorizontalAlignments HorizontalAlignment { get; }
    public ComponentWidths ComponentWidth { get; }
    public TagType Type { get; }

    public EmbedWidgetViewModel(EmbedWidgetProperties props, EmbedContent embed)
    {
        HTML = new HtmlString(embed.EmbedContentHTML);
        HorizontalAlignment = props.HorizontalAlignmentParsed;
        ComponentWidth = props.ComponentWidthParsed;

        Type = embed.EmbedContentType
            .TryFirst()
            .Map(tag =>
            {
                if (tag.Identifier == SystemTaxonomies.EmbedTypeTaxonomy.YouTube.TagGUID
                    || tag.Identifier == SystemTaxonomies.EmbedTypeTaxonomy.YouTube.TagGUID)
                {
                    return TagType.Video;
                }
                if (tag.Identifier == SystemTaxonomies.EmbedTypeTaxonomy.LinkedIn.TagGUID)
                {
                    return TagType.Social;
                }
                return TagType.Other;
            })
            .GetValueOrDefault(TagType.Other);
    }
}

