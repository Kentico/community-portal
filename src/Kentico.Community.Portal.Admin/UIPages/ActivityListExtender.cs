using CMS.Activities;
using CMS.ContentEngine;
using CMS.DataEngine;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(ActivityListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ActivityListExtender : PageExtender<ActivityList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        Page.PageConfiguration.QueryModifiers.AddModifier((query, s) =>
            query
                .AddColumn(nameof(ActivityTypeInfo.ActivityTypeName))
                .AddColumn(nameof(ChannelInfo.ChannelName)));

        Page.PageConfiguration.FilterFormModel = new ActivityListFilter();
    }
}

public class ActivityListFilter
{
    [GeneralSelectorComponent(
        dataProviderType: typeof(ActivityTypeGeneralSelectorDataProvider),
        Label = "Activity types",
        Placeholder = "Any",
        Order = 0
    )]
    [FilterCondition(
        BuilderType = typeof(GeneralWhereInWhereConditionBuilder),
        ColumnName = nameof(ActivityTypeInfo.ActivityTypeName)
    )]
    public IEnumerable<string> ActivityTypes { get; set; } = [];

    [GeneralSelectorComponent(
        dataProviderType: typeof(WebsiteChannelGeneralSelectorDataProvider),
        Label = "Channels",
        Placeholder = "Any",
        Order = 0
    )]
    [FilterCondition(
        BuilderType = typeof(GeneralWhereInWhereConditionBuilder),
        ColumnName = nameof(ChannelInfo.ChannelName)
    )]
    public IEnumerable<string> Channels { get; set; } = [];

    [TextInputComponent(
        Label = "Activity value",
        Order = 1,
        ExplanationText = "Matches an activity's custom value")]
    [FilterCondition(
        BuilderType = typeof(WhereLikeConditionBuilder),
        ColumnName = nameof(ActivityInfo.ActivityValue)
    )]
    public string Value { get; set; } = "";
}

public class ActivityTypeGeneralSelectorDataProvider(IInfoProvider<ActivityTypeInfo> languageProvider)
    : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<ActivityTypeInfo> languageProvider = languageProvider;

    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Inavlid", Value = "" };

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var items = await languageProvider.Get()
            .GetEnumerableTypedResultAsync();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            items = items.Where(i => i.ActivityTypeDisplayName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = items.Select(i => new ObjectSelectorListItem<string> { IsValid = true, Text = i.ActivityTypeDisplayName, Value = i.ActivityTypeName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken)
    {
        var items = await languageProvider.Get()
            .GetEnumerableTypedResultAsync();

        return (selectedValues ?? []).Select(GetSelectedItemByValue(items));

    }

    private Func<string, ObjectSelectorListItem<string>> GetSelectedItemByValue(IEnumerable<ActivityTypeInfo> languages) =>
        (string activityTypeName) => languages
            .TryFirst(c => string.Equals(c.ActivityTypeName, activityTypeName))
            .Map(c => new ObjectSelectorListItem<string> { IsValid = true, Text = c.ActivityTypeDisplayName, Value = c.ActivityTypeName })
            .GetValueOrDefault(InvalidItem);
}

