using System.Collections.Immutable;
using CMS.DataEngine;
using CMS.Membership;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.Widgets.Forms;
using Kentico.Community.Portal.Web.Components.Widgets.MVPActivityList;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: MVPActivityListWidget.IDENTIFIER,
    name: MVPActivityListWidget.NAME,
    viewComponentType: typeof(MVPActivityListWidget),
    propertiesType: typeof(MVPActivityListWidgetProperties),
    Description = "Lists all MVP activities submitted for the current member",
    IconClass = KenticoIcons.CHECKLIST)]

namespace Kentico.Community.Portal.Web.Components.Widgets.MVPActivityList;

public class MVPActivityListWidget(IMediator mediator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.MVPActivityList";
    public const string NAME = "MVP activity list";

    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<MVPActivityListWidgetProperties> _)
    {
        var communityMemberID = CommunityMember.GetMemberIDFromClaim(HttpContext);
        var resp = await mediator.Send(new MVPActivitiesQuery());
        var bizForm = FormParser.GetFormByClassName(FormClassName.From(MVPActivityItem.CLASS_NAME));
        if (!bizForm.TryGetValue(out var form))
        {
            return View("~/Components/ComponentError.cshtml");
        }

        return View("~/Components/Widgets/Activities/MVPActivityList.cshtml", new MVPActivityListWidgetViewModel(resp, communityMemberID.Value)
        {
            ActivitTypesMap = FormParser.GetFormFieldOptions(form, nameof(MVPActivityItem.ActivityType)),
            ImpactMap = FormParser.GetFormFieldOptions(form, nameof(MVPActivityItem.Impact)),
            EfforMap = FormParser.GetFormFieldOptions(form, nameof(MVPActivityItem.Effort)),
            SatisfactionMap = FormParser.GetFormFieldOptions(form, nameof(MVPActivityItem.Satisfaction))
        });
    }
}

public class MVPActivityListWidgetProperties : BaseWidgetProperties { }

public class MVPActivityListWidgetViewModel : BaseWidgetViewModel
{
    public MVPActivityListWidgetViewModel(MVPActivitiesQueryResponse resp, int currentMemberID)
    {
        MyActivities = resp.ActivitiesByMember.TryGetValue(currentMemberID, out var myActivities)
            ? myActivities
            : [];
        AllActivities = resp.AllActivities;
        MemberNames = resp.MemberNames;
    }

    protected override string WidgetName { get; } = MVPActivityListWidget.NAME;

    public IReadOnlyList<MVPActivityItem> AllActivities { get; }
    public IReadOnlyList<MVPActivityItem> MyActivities { get; }
    public Dictionary<int, string> MemberNames { get; }
    public required IReadOnlyDictionary<string, string> ActivitTypesMap { get; init; }
    public required IReadOnlyDictionary<string, string> ImpactMap { get; init; }
    public required IReadOnlyDictionary<string, string> EfforMap { get; init; }
    public required IReadOnlyDictionary<string, string> SatisfactionMap { get; init; }

    public string ActivityType(MVPActivityItem item) =>
    ActivitTypesMap.TryGetValue(item.ActivityType, out string? typeVal)
        ? typeVal
        : item.ActivityType;
    public string Impact(MVPActivityItem item) =>
        ImpactMap.TryGetValue(item.Impact, out string? impact) ? impact : item.Impact;

    public string Effort(MVPActivityItem item) =>
        EfforMap.TryGetValue(item.Effort, out string? effort) ? effort : item.Effort;

    public string Satisfaction(MVPActivityItem item) =>
        SatisfactionMap.TryGetValue(item.Satisfaction, out string? Satisfaction)
            ? Satisfaction
            : item.Satisfaction;

    public string MemberName(MVPActivityItem item) =>
        MemberNames.TryGetValue(item.MemberID, out string? name) ? name : "";
}

public record MVPActivitiesQuery : IQuery<MVPActivitiesQueryResponse>;
public record MVPActivitiesQueryResponse(
    Dictionary<int, ImmutableList<MVPActivityItem>> ActivitiesByMember,
    ImmutableList<MVPActivityItem> AllActivities,
    Dictionary<int, string> MemberNames
);
public class MVPActivitiesQueryHandler(
    DataItemQueryTools tools,
    TimeProvider time,
    IInfoProvider<MemberInfo> memberProvider) : DataItemQueryHandler<MVPActivitiesQuery, MVPActivitiesQueryResponse>(tools)
{
    private readonly TimeProvider time = time;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<MVPActivitiesQueryResponse> Handle(MVPActivitiesQuery request, CancellationToken cancellationToken = default)
    {
        var items = await BizFormItemProvider.GetItems<MVPActivityItem>()
            .WhereGreaterOrEquals(nameof(MVPActivityItem.ActivityDate), new DateTime(time.GetLocalNow().Year, 1, 1))
            .OrderByDescending(nameof(MVPActivityItem.ActivityDate))
            .GetEnumerableTypedResultAsync();

        var members = await memberProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberID), items.Select(i => i.MemberID).Distinct())
            .GetEnumerableTypedResultAsync();

        var memberNames = members.Select(m => m.AsCommunityMember()).ToDictionary(m => m.Id, m => m.DisplayName);

        return new(
            items.GroupBy(i => i.MemberID).ToDictionary(g => g.Key, g => g.ToImmutableList()),
            [.. items],
            memberNames
        );
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MVPActivitiesQuery query, MVPActivitiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(MVPActivityItem.CLASS_NAME);
}
