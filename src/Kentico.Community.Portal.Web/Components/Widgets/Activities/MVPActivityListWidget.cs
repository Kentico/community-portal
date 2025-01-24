using System.Security.Claims;
using CMS.DataEngine;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.Widgets.MVPActivityList;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: MVPActivityListWidget.IDENTIFIER,
    name: MVPActivityListWidget.NAME,
    viewComponentType: typeof(MVPActivityListWidget),
    propertiesType: typeof(MVPActivityListWidgetProperties),
    Description = "Lists all MVP activities submitted for the current member",
    IconClass = KenticoIcons.CHECKLIST)]

namespace Kentico.Community.Portal.Web.Components.Widgets.MVPActivityList;

public class MVPActivityListWidget(IMediator mediator, UserManager<CommunityMember> userManager) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.MVPActivityList";
    public const string NAME = "MVP activity list";

    private readonly IMediator mediator = mediator;
    private readonly UserManager<CommunityMember> userManager = userManager;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<MVPActivityListWidgetProperties> _)
    {
        string? memberIDStr = User is ClaimsPrincipal user
            ? userManager.GetUserId(user)
            : null;
        int memberID = int.TryParse(memberIDStr, out int id)
            ? id
            : 0;
        var resp = await mediator.Send(new MVPActivitiesQuery(memberID));

        var dataClass = DataClassInfoProvider.GetDataClassInfo(MVPActivityItem.CLASS_NAME);
        var form = BizFormInfoProvider.GetBizFormInfoForClass(dataClass.ClassID);

        return Validate(
            resp,
            ParseFieldDataSource(form, nameof(MVPActivityItem.ActivityType)),
            ParseFieldDataSource(form, nameof(MVPActivityItem.Impact)),
            ParseFieldDataSource(form, nameof(MVPActivityItem.Effort)),
            ParseFieldDataSource(form, nameof(MVPActivityItem.Satisfaction)))
            .Match(
                vm => View("~/Components/Widgets/Activities/MVPActivityList.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private static Result<MVPActivityListWidgetViewModel, ComponentErrorViewModel> Validate(
        MVPActivitiesQueryResponse resp,
        IReadOnlyDictionary<string, string> activitTypesMap,
        IReadOnlyDictionary<string, string> impactMap,
        IReadOnlyDictionary<string, string> effortMap,
        IReadOnlyDictionary<string, string> satisfactionMap) =>
        new MVPActivityListWidgetViewModel(resp.Items)
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

public class MVPActivityListWidgetProperties : BaseWidgetProperties { }

public class MVPActivityListWidgetViewModel(IReadOnlyList<MVPActivityItem> items) : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = MVPActivityListWidget.NAME;

    public IReadOnlyList<MVPActivityItem> Items { get; } = items;
    public required IReadOnlyDictionary<string, string> ActivitTypesMap { get; init; }
    public required IReadOnlyDictionary<string, string> ImpactMap { get; init; }
    public required IReadOnlyDictionary<string, string> EfforMap { get; init; }
    public required IReadOnlyDictionary<string, string> SatisfactionMap { get; init; }
}

public record MVPActivitiesQuery(int MemberID) : IQuery<MVPActivitiesQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => MemberID.ToString();
}

public record MVPActivitiesQueryResponse(IReadOnlyList<MVPActivityItem> Items);
public class MVPActivitiesQueryQueryHandler(DataItemQueryTools tools, TimeProvider time) : DataItemQueryHandler<MVPActivitiesQuery, MVPActivitiesQueryResponse>(tools)
{
    private readonly TimeProvider time = time;

    public override async Task<MVPActivitiesQueryResponse> Handle(MVPActivitiesQuery request, CancellationToken cancellationToken = default)
    {
        var items = await BizFormItemProvider.GetItems<MVPActivityItem>()
            .WhereEquals(nameof(MVPActivityItem.MemberID), request.MemberID)
            .WhereGreaterOrEquals(nameof(MVPActivityItem.ActivityDate), new DateTime(time.GetLocalNow().Year, 1, 1))
            .OrderByDescending(nameof(MVPActivityItem.ActivityDate))
            .GetEnumerableTypedResultAsync();

        return new([.. items]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MVPActivitiesQuery query, MVPActivitiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(MVPActivityItem.CLASS_NAME);
}
