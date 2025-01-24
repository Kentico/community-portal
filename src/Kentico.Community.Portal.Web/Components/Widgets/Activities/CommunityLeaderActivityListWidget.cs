using System.Security.Claims;
using CMS.DataEngine;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.Widgets.CommunityLeaderActivityList;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CommunityLeaderActivityListWidget.IDENTIFIER,
    name: CommunityLeaderActivityListWidget.NAME,
    viewComponentType: typeof(CommunityLeaderActivityListWidget),
    propertiesType: typeof(CommunityLeaderActivityListWidgetProperties),
    Description = "Lists all Community Leader activities submitted for the current member",
    IconClass = KenticoIcons.CHECKLIST)]

namespace Kentico.Community.Portal.Web.Components.Widgets.CommunityLeaderActivityList;

public class CommunityLeaderActivityListWidget(IMediator mediator, UserManager<CommunityMember> userManager) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.CommunityLeaderActivityList";
    public const string NAME = "CL activity list";

    private readonly IMediator mediator = mediator;
    private readonly UserManager<CommunityMember> userManager = userManager;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<CommunityLeaderActivityListWidgetProperties> _)
    {
        string? memberIDStr = User is ClaimsPrincipal user
            ? userManager.GetUserId(user)
            : null;
        int memberID = int.TryParse(memberIDStr, out int id)
            ? id
            : 0;
        var resp = await mediator.Send(new CommunityLeaderActivitiesQuery(memberID));

        var dataClass = DataClassInfoProvider.GetDataClassInfo(CommunityLeaderActivityItem.CLASS_NAME);
        var form = BizFormInfoProvider.GetBizFormInfoForClass(dataClass.ClassID);

        return Validate(
            resp,
            ParseFieldDataSource(form, nameof(CommunityLeaderActivityItem.ActivityType)),
            ParseFieldDataSource(form, nameof(CommunityLeaderActivityItem.Impact)),
            ParseFieldDataSource(form, nameof(CommunityLeaderActivityItem.Effort)),
            ParseFieldDataSource(form, nameof(CommunityLeaderActivityItem.Satisfaction)))
            .Match(
                vm => View("~/Components/Widgets/Activities/CommunityLeaderActivityList.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private static Result<CommunityLeaderActivityListWidgetViewModel, ComponentErrorViewModel> Validate(
        CommunityLeaderActivitiesQueryResponse resp,
        IReadOnlyDictionary<string, string> activitTypesMap,
        IReadOnlyDictionary<string, string> impactMap,
        IReadOnlyDictionary<string, string> effortMap,
        IReadOnlyDictionary<string, string> satisfactionMap) =>
        new CommunityLeaderActivityListWidgetViewModel(resp.Items)
        {
            ActivitTypesMap = activitTypesMap,
            ImpactMap = impactMap,
            EfforMap = effortMap,
            SatisfactionMap = satisfactionMap
        };

    private static Dictionary<string, string> ParseFieldDataSource(BizFormInfo form, string fieldName)
    {
        object? dataSource = form.Form.GetFormField(fieldName).Settings["DataSource"];

        if (dataSource is not string srcString || string.IsNullOrWhiteSpace(srcString))
        {
            return [];
        }

        var lookup = new Dictionary<string, string>();
        string[] lines = srcString.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string[] parts = line.Split(';');
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                string value = parts[1].Trim();
                lookup[key] = value;
            }
        }

        return lookup;
    }
}

public class CommunityLeaderActivityListWidgetProperties : BaseWidgetProperties { }

public class CommunityLeaderActivityListWidgetViewModel(IReadOnlyList<CommunityLeaderActivityItem> items) : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = CommunityLeaderActivityListWidget.NAME;

    public IReadOnlyList<CommunityLeaderActivityItem> Items { get; } = items;
    public required IReadOnlyDictionary<string, string> ActivitTypesMap { get; init; }
    public required IReadOnlyDictionary<string, string> ImpactMap { get; init; }
    public required IReadOnlyDictionary<string, string> EfforMap { get; init; }
    public required IReadOnlyDictionary<string, string> SatisfactionMap { get; init; }
}

public record CommunityLeaderActivitiesQuery(int MemberID) : IQuery<CommunityLeaderActivitiesQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => MemberID.ToString();
}

public record CommunityLeaderActivitiesQueryResponse(IReadOnlyList<CommunityLeaderActivityItem> Items);
public class CommunityLeaderActivitiesQueryHandler(DataItemQueryTools tools, TimeProvider time) : DataItemQueryHandler<CommunityLeaderActivitiesQuery, CommunityLeaderActivitiesQueryResponse>(tools)
{
    private readonly TimeProvider time = time;

    public override async Task<CommunityLeaderActivitiesQueryResponse> Handle(CommunityLeaderActivitiesQuery request, CancellationToken cancellationToken = default)
    {
        var items = await BizFormItemProvider.GetItems<CommunityLeaderActivityItem>()
            .WhereEquals(nameof(CommunityLeaderActivityItem.MemberID), request.MemberID)
            .WhereGreaterOrEquals(nameof(CommunityLeaderActivityItem.ActivityDate), new DateTime(time.GetLocalNow().Year, 1, 1))
            .OrderByDescending(nameof(CommunityLeaderActivityItem.ActivityDate))
            .GetEnumerableTypedResultAsync();

        return new([.. items]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(CommunityLeaderActivitiesQuery query, CommunityLeaderActivitiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(CommunityLeaderActivityItem.CLASS_NAME);
}
