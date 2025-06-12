using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Video;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: VideoWidget.IDENTIFIER,
    viewComponentType: typeof(VideoWidget),
    name: VideoWidget.NAME,
    propertiesType: typeof(VideoWidgetProperties),
    Description = "Adds a video to the page",
    IconClass = KenticoIcons.TRIANGLE_RIGHT)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Video;

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
                vm => View("~/Components/PageBuilder/Widgets/Video/Video.cshtml", vm),
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

        return new VideoWidgetViewModel(vid, props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class VideoWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        VideoContent.CONTENT_TYPE_NAME,
        Label = "Selected video",
        MinimumItems = 1,
        MaximumItems = 1,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> SelectedVideos { get; set; } = [];

    [DropDownComponent(
        Label = "Alignment",
        ExplanationText = "The alignment of the video",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<VideoAlignments>),
        Order = 4
    )]
    public string VideoAlignment { get; set; } = nameof(VideoAlignments.Center);
    public VideoAlignments VideoAlignmentParsed => EnumDropDownOptionsProvider<VideoAlignments>.Parse(VideoAlignment, VideoAlignments.Center);

    [DropDownComponent(
        Label = "Size",
        ExplanationText = "The size of the video",
        Tooltip = "Select a size",
        DataProviderType = typeof(EnumDropDownOptionsProvider<VideoSizes>),
        Order = 5
    )]
    public string Size { get; set; } = nameof(VideoSizes.Full_Width);
    public VideoSizes SizeParsed => EnumDropDownOptionsProvider<VideoSizes>.Parse(Size, VideoSizes.Full_Width);


    [CheckBoxComponent(
        Label = "Show description as caption?",
        ExplanationText = "If true, a caption will appear below the video, populated by the video's description field.",
        Order = 6
    )]
    public bool ShowDescriptionAsCaption { get; set; } = false;
}

public class VideoWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = VideoWidget.NAME;

    public VideoContent Video { get; }
    public VideoAlignments Alignment { get; }
    public VideoSizes Size { get; }
    public bool ShowDescriptionAsCaption { get; }

    public VideoWidgetViewModel(VideoContent video, VideoWidgetProperties props)
    {
        Video = video;
        Alignment = props.VideoAlignmentParsed;
        Size = props.SizeParsed;
        ShowDescriptionAsCaption = props.ShowDescriptionAsCaption;
    }
};

public enum VideoAlignments
{
    Left,
    Center,
    Right
}

public enum VideoSizes
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
