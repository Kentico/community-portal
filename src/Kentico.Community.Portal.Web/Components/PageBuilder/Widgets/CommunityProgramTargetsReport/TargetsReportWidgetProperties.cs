using System.ComponentModel.DataAnnotations;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport;

public class TargetsReportWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Heading",
        ExplanationText = "Optional widget heading",
        Order = 1)]
    public string Heading { get; set; } = "";

    [NumberInputComponent(Label = "MVP: annual activities target", Order = 10)]
    [Range(0, 10000)]
    public int MvpActivitiesTargetYear { get; set; } = 30;

    [NumberInputComponent(Label = "MVP: annual discussions created target", Order = 11)]
    [Range(0, 10000)]
    public int MvpDiscussionsCreatedTargetYear { get; set; } = 6;

    [NumberInputComponent(Label = "MVP: quarterly social posts target", Order = 12)]
    [Range(0, 10000)]
    public int MvpSocialPostsTargetQuarter { get; set; } = 4;

    [NumberInputComponent(Label = "MVP: quarterly total activities target", Order = 13)]
    [Range(0, 10000)]
    public int MvpTotalActivitiesTargetQuarter { get; set; } = 6;

    [NumberInputComponent(Label = "MVP: annual Kentico feedback activities target", Order = 14)]
    [Range(0, 10000)]
    public int MvpKenticoFeedbackActivitiesTargetYear { get; set; } = 8;

    [NumberInputComponent(Label = "Community Leader: annual activities target", Order = 20)]
    [Range(0, 10000)]
    public int CommunityLeaderActivitiesTargetYear { get; set; } = 30;

    [NumberInputComponent(Label = "Community Leader: annual discussions created target", Order = 21)]
    [Range(0, 10000)]
    public int CommunityLeaderDiscussionsCreatedTargetYear { get; set; } = 12;

    [NumberInputComponent(Label = "Community Leader: quarterly social posts target", Order = 22)]
    [Range(0, 10000)]
    public int CommunityLeaderSocialPostsTargetQuarter { get; set; } = 3;

    [NumberInputComponent(Label = "Community Leader: quarterly total activities target", Order = 23)]
    [Range(0, 10000)]
    public int CommunityLeaderTotalActivitiesTargetQuarter { get; set; } = 4;

    [NumberInputComponent(Label = "Community Leader: annual Kentico feedback activities target", Order = 24)]
    [Range(0, 10000)]
    public int CommunityLeaderKenticoFeedbackActivitiesTargetYear { get; set; } = 4;

    public int? EmulatedMemberID { get; set; } = null;
}
