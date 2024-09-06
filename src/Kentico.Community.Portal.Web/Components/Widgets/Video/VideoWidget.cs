using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Components.Widgets.Video;
using CMS.ContentEngine;
using MediatR;

[assembly: RegisterWidget(
    identifier: VideoWidget.IDENTIFIER,
    viewComponentType: typeof(VideoWidget),
    name: VideoWidget.NAME,
    propertiesType: typeof(VideoWidgetProperties),
    Description = "Adds a video to the page",
    IconClass = "icon-camera")]

namespace Kentico.Community.Portal.Web.Components.Widgets.Video;

public class VideoWidget(IMediator mediator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.VideoWidget";
    public const string NAME = "Video";

    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<VideoWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var contentItemGUID = props.SelectedVideos.Select(i => i.Identifier).FirstOrDefault();
        var video = await mediator.Send(new VideoContentByGUIDQuery(contentItemGUID));

        return Validate(props, video)
            .Match(
                vm => View("~/Components/Widgets/Video/Video.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private static Result<VideoWidgetViewModel, ComponentErrorViewModel> Validate(VideoWidgetProperties props, Maybe<VideoContent> video)
    {
        if (props.SelectedVideos.FirstOrDefault() is null)
        {
            return Result.Failure<VideoWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No video has been selected."));
        }

        if (!video.TryGetValue(out var vid) || vid.VideoContentAsset is null)
        {
            return Result.Failure<VideoWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or video file no longer exists."));
        }

        return new VideoWidgetViewModel(video.GetValueOrDefault(), props.VideoAlignmentParsed);
    }
}

public class VideoWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        VideoContent.CONTENT_TYPE_NAME,
        Label = "Selected video",
        Order = 1)]
    public IEnumerable<ContentItemReference> SelectedVideos { get; set; } = [];

    [DropDownComponent(
        Label = "Video Alignment",
        ExplanationText = "The alignment of the video",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Alignments>),
        Order = 2
    )]
    public string VideoAlignment { get; set; } = nameof(Alignments.Left);
    public Alignments VideoAlignmentParsed => EnumDropDownOptionsProvider<Alignments>.Parse(VideoAlignment, Alignments.Left);
}

public class VideoWidgetViewModel(VideoContent video, Alignments alignment) : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = VideoWidget.NAME;

    public VideoContent Video { get; } = video;
    public Alignments Alignment { get; } = alignment;
};

public enum Alignments
{
    Left,
    Center
}
