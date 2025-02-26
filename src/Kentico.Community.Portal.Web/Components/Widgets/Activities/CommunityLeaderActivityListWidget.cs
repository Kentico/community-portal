using System.Collections.Immutable;
using CMS.DataEngine;
using CMS.Membership;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.Widgets.CommunityLeaderActivityList;
using Kentico.Community.Portal.Web.Components.Widgets.Forms;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CommunityLeaderActivityListWidget.IDENTIFIER,
    name: CommunityLeaderActivityListWidget.NAME,
    viewComponentType: typeof(CommunityLeaderActivityListWidget),
    propertiesType: typeof(CommunityLeaderActivityListWidgetProperties),
    Description = "Lists all Community Leader activities",
    IconClass = KenticoIcons.CHECKLIST)]

namespace Kentico.Community.Portal.Web.Components.Widgets.CommunityLeaderActivityList;

public class CommunityLeaderActivityListWidget(IMediator mediator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.CommunityLeaderActivityList";
    public const string NAME = "CL activity list";

    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<CommunityLeaderActivityListWidgetProperties> _)
    {
        var communityMemberID = CommunityMember.GetMemberIDFromClaim(HttpContext);
        var resp = await mediator.Send(new CommunityLeaderActivitiesQuery());

        var bizForm = FormParser.GetFormByClassName(FormClassName.From(CommunityLeaderActivityItem.CLASS_NAME));

        if (!bizForm.TryGetValue(out var form))
        {
            return View("~/Components/ComponentError.cshtml");
        }

        return View("~/Components/Widgets/Activities/CommunityLeaderActivityList.cshtml",
            new CommunityLeaderActivityListWidgetViewModel(resp, communityMemberID.Value)
            {
                ActivitTypesMap = FormParser.GetFormFieldOptions(form, nameof(CommunityLeaderActivityItem.ActivityType)),
                ImpactMap = FormParser.GetFormFieldOptions(form, nameof(CommunityLeaderActivityItem.Impact)),
                EfforMap = FormParser.GetFormFieldOptions(form, nameof(CommunityLeaderActivityItem.Effort)),
                SatisfactionMap = FormParser.GetFormFieldOptions(form, nameof(CommunityLeaderActivityItem.Satisfaction))
            });
    }
}

public class CommunityLeaderActivityListWidgetProperties : BaseWidgetProperties { }

public class CommunityLeaderActivityListWidgetViewModel : BaseWidgetViewModel
{
    public CommunityLeaderActivityListWidgetViewModel(CommunityLeaderActivitiesQueryResponse resp, int currentMemberID)
    {
        MyActivities = resp.ActivitiesByMember.TryGetValue(currentMemberID, out var myActivities)
            ? myActivities
            : [];
        AllActivities = resp.AllActivities;
        MemberNames = resp.MemberNames;
    }

    protected override string WidgetName { get; } = CommunityLeaderActivityListWidget.NAME;

    public IReadOnlyList<CommunityLeaderActivityItem> AllActivities { get; }
    public IReadOnlyList<CommunityLeaderActivityItem> MyActivities { get; }
    public Dictionary<int, string> MemberNames { get; }
    public required IReadOnlyDictionary<string, string> ActivitTypesMap { get; init; }
    public required IReadOnlyDictionary<string, string> ImpactMap { get; init; }
    public required IReadOnlyDictionary<string, string> EfforMap { get; init; }
    public required IReadOnlyDictionary<string, string> SatisfactionMap { get; init; }

    public string ActivityType(CommunityLeaderActivityItem item) =>
        ActivitTypesMap.TryGetValue(item.ActivityType, out string? typeVal)
            ? typeVal
            : item.ActivityType;
    public string Impact(CommunityLeaderActivityItem item) =>
        ImpactMap.TryGetValue(item.Impact, out string? impact) ? impact : item.Impact;

    public string Effort(CommunityLeaderActivityItem item) =>
        EfforMap.TryGetValue(item.Effort, out string? effort) ? effort : item.Effort;

    public string Satisfaction(CommunityLeaderActivityItem item) =>
        SatisfactionMap.TryGetValue(item.Satisfaction, out string? Satisfaction)
            ? Satisfaction
            : item.Satisfaction;

    public string MemberName(CommunityLeaderActivityItem item) =>
        MemberNames.TryGetValue(item.MemberID, out string? name) ? name : "";
}

public record CommunityLeaderActivitiesQuery : IQuery<CommunityLeaderActivitiesQueryResponse>;
public record CommunityLeaderActivitiesQueryResponse(
    Dictionary<int, ImmutableList<CommunityLeaderActivityItem>> ActivitiesByMember,
    ImmutableList<CommunityLeaderActivityItem> AllActivities,
    Dictionary<int, string> MemberNames);
public class CommunityLeaderActivitiesQueryHandler(
    DataItemQueryTools tools,
    TimeProvider time,
    IInfoProvider<MemberInfo> memberProvider) : DataItemQueryHandler<CommunityLeaderActivitiesQuery, CommunityLeaderActivitiesQueryResponse>(tools)
{
    private readonly TimeProvider time = time;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<CommunityLeaderActivitiesQueryResponse> Handle(CommunityLeaderActivitiesQuery request, CancellationToken cancellationToken = default)
    {
        var items = await BizFormItemProvider.GetItems<CommunityLeaderActivityItem>()
            .WhereGreaterOrEquals(nameof(CommunityLeaderActivityItem.ActivityDate), new DateTime(time.GetLocalNow().Year, 1, 1))
            .OrderByDescending(nameof(CommunityLeaderActivityItem.ActivityDate))
            .GetEnumerableTypedResultAsync();

        var members = await memberProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberID), items.Select(i => i.MemberID).Distinct())
            .GetEnumerableTypedResultAsync();

        var memberNames = members.Select(m => m.AsCommunityMember()).ToDictionary(m => m.Id, m => m.DisplayName);

        return new(
            items.GroupBy(i => i.MemberID).ToDictionary(g => g.Key, g => g.ToImmutableList()),
            [.. items],
            memberNames);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(CommunityLeaderActivitiesQuery query, CommunityLeaderActivitiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(CommunityLeaderActivityItem.CLASS_NAME);
}
