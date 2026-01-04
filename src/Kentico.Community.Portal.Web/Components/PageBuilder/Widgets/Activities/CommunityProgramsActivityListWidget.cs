using System;
using System.Collections.Immutable;
using CMS.DataEngine;
using CMS.Membership;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramsActivityList;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Forms;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CommunityProgramsActivityListWidget.IDENTIFIER,
    name: CommunityProgramsActivityListWidget.NAME,
    viewComponentType: typeof(CommunityProgramsActivityListWidget),
    propertiesType: typeof(CommunityProgramsActivityListWidgetProperties),
    Description = "Lists all activities from MVPs and Community Leaders",
    IconClass = KenticoIcons.CHECKLIST)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramsActivityList;

public class CommunityProgramsActivityListWidget(IMediator mediator, TimeProvider timeProvider) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.CommunityProgramsActivityList";
    public const string NAME = "Community programs activity list";

    private readonly IMediator mediator = mediator;
    private readonly TimeProvider timeProvider = timeProvider;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<CommunityProgramsActivityListWidgetProperties> _)
    {
        var communityMemberID = CommunityMember.GetMemberIDFromClaim(HttpContext);

        var formResult = FormParser.GetFormByClassName(FormClassName.From(CommunityProgramActivity_2026Item.CLASS_NAME));

        if (!formResult.TryGetValue(out var form))
        {
            return View("~/Components/ComponentError.cshtml");
        }

        var activityTypesMap = FormParser.GetFormFieldOptions(form, nameof(CommunityProgramActivity_2026Item.ActivityType));
        var impactMap = FormParser.GetFormFieldOptions(form, nameof(CommunityProgramActivity_2026Item.Impact));
        var effortMap = FormParser.GetFormFieldOptions(form, nameof(CommunityProgramActivity_2026Item.Effort));
        var satisfactionMap = FormParser.GetFormFieldOptions(form, nameof(CommunityProgramActivity_2026Item.Satisfaction));

        CommunityProgramsActivitiesQueryResponse resp;

#if DEBUG
        if (HttpContext.Request.Query.ContainsKey("fakeActivities"))
        {
            var seed = HttpContext.Request.Query.TryGetValue("seed", out var seedValue) && int.TryParse(seedValue, out var parsedSeed)
            ? parsedSeed
            : 0;

            resp = CommunityProgramsActivityListFakeData.GenerateFakeActivitiesResponse(
            count: 1300,
            year: timeProvider.GetLocalNow().Year,
            activityTypesMap: activityTypesMap,
            impactMap: impactMap,
            effortMap: effortMap,
            satisfactionMap: satisfactionMap,
            seed: seed);
        }
        else
        {
            resp = await mediator.Send(new CommunityProgramsActivitiesQuery());
        }
#else
        resp = await mediator.Send(new CommunityProgramsActivitiesQuery());
#endif

        return View("~/Components/PageBuilder/Widgets/Activities/CommunityProgramsActivityList.cshtml",
            new CommunityProgramsActivityListWidgetViewModel(resp, communityMemberID.Value)
            {
                ActivitTypesMap = activityTypesMap,
                ImpactMap = impactMap,
                EfforMap = effortMap,
                SatisfactionMap = satisfactionMap
            });
    }
}

public class CommunityProgramsActivityListWidgetProperties : BaseWidgetProperties { }

public class CommunityProgramsActivityListWidgetViewModel : BaseWidgetViewModel
{
    public CommunityProgramsActivityListWidgetViewModel(CommunityProgramsActivitiesQueryResponse resp, int currentMemberID)
    {
        MyActivities = resp.ActivitiesByMember.TryGetValue(currentMemberID, out var myActivities)
            ? myActivities
            : [];
        AllActivities = resp.AllActivities;
        MemberNames = resp.MemberNames;
    }

    protected override string WidgetName { get; } = CommunityProgramsActivityListWidget.NAME;

    public IReadOnlyList<CommunityProgramsActivityListItem> AllActivities { get; }
    public IReadOnlyList<CommunityProgramsActivityListItem> MyActivities { get; }

    // NOTE: Intentionally named to match existing widget conventions.
    public Dictionary<int, CommunityProgramsMemberName> MemberNames { get; }

    public required IReadOnlyDictionary<string, string> ActivitTypesMap { get; init; }
    public required IReadOnlyDictionary<string, string> ImpactMap { get; init; }
    public required IReadOnlyDictionary<string, string> EfforMap { get; init; }
    public required IReadOnlyDictionary<string, string> SatisfactionMap { get; init; }

    public IReadOnlyList<ProgramStatuses> ProgramStatusOptions { get; } = [ProgramStatuses.MVP, ProgramStatuses.CommunityLeader];

    public string ActivityType(CommunityProgramsActivityListItem item) =>
        ActivitTypesMap.TryGetValue(item.ActivityType, out string? typeVal)
            ? typeVal
            : item.ActivityType;

    public string Impact(CommunityProgramsActivityListItem item) =>
        ImpactMap.TryGetValue(item.Impact, out string? impact) ? impact : item.Impact;

    public string Effort(CommunityProgramsActivityListItem item) =>
        EfforMap.TryGetValue(item.Effort, out string? effort) ? effort : item.Effort;

    public string Satisfaction(CommunityProgramsActivityListItem item) =>
        SatisfactionMap.TryGetValue(item.Satisfaction, out string? satisfaction)
            ? satisfaction
            : item.Satisfaction;

    public string MemberName(CommunityProgramsActivityListItem item) =>
        MemberNames.TryGetValue(item.MemberID, out var member) ? member.Name : "";

    public string MemberLabel(CommunityProgramsMemberName member) =>
        member.ProgramStatus is ProgramStatuses.None
            ? member.Name
            : $"{member.Name} ({ProgramStatusLabel(member.ProgramStatus)})";

    public string ProgramStatusLabel(ProgramStatuses status) =>
        status switch
        {
            ProgramStatuses.CommunityLeader => "Community Leader",
            _ => status.ToString()
        };
}

public record CommunityProgramsActivityListItem(
    int MemberID,
    ProgramStatuses ProgramStatus,
    DateTime ActivityDate,
    string ActivityType,
    string URL,
    string ShortDescription,
    string Attendees,
    string Impact,
    string Effort,
    string Satisfaction);

public record CommunityProgramsMemberName(string Name, ProgramStatuses ProgramStatus);

public record CommunityProgramsActivitiesQuery : IQuery<CommunityProgramsActivitiesQueryResponse>;

public record CommunityProgramsActivitiesQueryResponse(
    Dictionary<int, ImmutableList<CommunityProgramsActivityListItem>> ActivitiesByMember,
    ImmutableList<CommunityProgramsActivityListItem> AllActivities,
    Dictionary<int, CommunityProgramsMemberName> MemberNames);

public class CommunityProgramsActivitiesQueryHandler(
    DataItemQueryTools tools,
    TimeProvider time,
    IInfoProvider<MemberInfo> memberProvider) : DataItemQueryHandler<CommunityProgramsActivitiesQuery, CommunityProgramsActivitiesQueryResponse>(tools)
{
    private readonly TimeProvider time = time;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<CommunityProgramsActivitiesQueryResponse> Handle(CommunityProgramsActivitiesQuery request, CancellationToken cancellationToken = default)
    {
        var yearStart = new DateTime(time.GetLocalNow().Year, 1, 1);

        var items = await BizFormItemProvider.GetItems<CommunityProgramActivity_2026Item>()
            .WhereGreaterOrEquals(nameof(CommunityProgramActivity_2026Item.ActivityDate), yearStart)
            .OrderByDescending(nameof(CommunityProgramActivity_2026Item.ActivityDate))
            .GetEnumerableTypedResultAsync();

        var memberIds = items.Select(i => i.MemberID)
            .Distinct()
            .ToArray();

        var members = await memberProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberID), memberIds)
            .GetEnumerableTypedResultAsync();

        var memberNames = members
            .Select(m => m.AsCommunityMember())
            .ToDictionary(m => m.Id, m => new CommunityProgramsMemberName(m.DisplayName, m.ProgramStatus));

        ProgramStatuses GetProgramStatus(int memberId) =>
            memberNames.TryGetValue(memberId, out var member)
                ? member.ProgramStatus
                : ProgramStatuses.None;

        var ordered = items
            .Select(item => new CommunityProgramsActivityListItem(
                MemberID: item.MemberID,
                ProgramStatus: GetProgramStatus(item.MemberID),
                ActivityDate: item.ActivityDate,
                ActivityType: item.ActivityType,
                URL: item.URLInput,
                ShortDescription: item.ShortDescription,
                Attendees: item.Attendees,
                Impact: item.Impact,
                Effort: item.Effort,
                Satisfaction: item.Satisfaction))
            .ToImmutableList();

        return new(
            ordered.GroupBy(i => i.MemberID).ToDictionary(g => g.Key, g => g.ToImmutableList()),
            ordered,
            memberNames);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(CommunityProgramsActivitiesQuery query, CommunityProgramsActivitiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllObjects(CommunityProgramActivity_2026Item.CLASS_NAME)
            .AllObjects(MemberInfo.OBJECT_TYPE);
}
