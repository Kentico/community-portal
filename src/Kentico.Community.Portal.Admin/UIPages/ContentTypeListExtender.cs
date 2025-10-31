using CMS.ContentEngine;
using CMS.DataEngine;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Base.UIPages;
using Microsoft.Extensions.Logging;

[assembly: PageExtender(typeof(ContentTypeListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ContentTypeListExtender(ILogger<ContentTypeListExtender> logger) : PageExtender<ContentTypeList>
{
    private readonly ILogger<ContentTypeListExtender> logger = logger;
    private static readonly HashSet<string> loggedOnce = [];

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var pageConfig = Page.PageConfiguration;

        if (pageConfig.FilterFormModel is null)
        {
            pageConfig.FilterFormModel = new ContentTypeListMultiFilter();
        }
        else
        {
            if (loggedOnce.Add("DUPLICATE_FILTER"))
            {
                logger.LogWarning(new EventId(0, "DUPLICATE_FILTER"), "ContentTypeListExtender filter already present; skipping duplicate insertion");
            }
        }

        var configs = pageConfig.ColumnConfigurations;

        /*
         * If this column is added to the default configuration in the future
         * we don't want to override it
         */
        if (configs.FirstOrDefault(c => string.Equals(c.Name, nameof(ContentTypeInfo.ClassLastModified), StringComparison.OrdinalIgnoreCase)) is null)
        {
            configs
                .TryFirst(c => string.Equals(c.Name, nameof(ContentTypeInfo.ClassContentTypeType), StringComparison.OrdinalIgnoreCase))
                .Execute(c =>
                {
                    int index = configs.IndexOf(c);
                    index = Math.Min(index + 1, configs.Count - 1);

                    configs.Insert(index, new ColumnConfiguration
                    {
                        Name = nameof(ContentTypeInfo.ClassLastModified),
                        Caption = "Modified",
                        MinWidth = 19,
                        MaxWidth = 50,
                        Sorting = new SortingConfiguration
                        {
                            Sortable = true,
                            DefaultDirection = SortTypeEnum.Desc
                        },
                    });
                });
        }
    }
}

public class ContentTypeListMultiFilter
{
    [GeneralSelectorComponent(
        dataProviderType: typeof(ContentTypeTypeGeneralSelectorDataProvider),
        Label = "Content Type Uses",
        Placeholder = "Any",
        Order = 0
    )]
    [FilterCondition(
        BuilderType = typeof(ContentTypeTypeWhereConditionBuilder),
        ColumnName = nameof(ContentTypeInfo.ClassContentTypeType)
    )]
    public IEnumerable<string> ContentTypeUses { get; set; } = [];
}

public class ContentTypeTypeGeneralSelectorDataProvider
    : IGeneralSelectorDataProvider
{
    private ObjectSelectorListItem<string> Reusable { get; } = new()
    {
        Value = ClassContentTypeType.REUSABLE,
        Text = ClassContentTypeType.REUSABLE,
        IsValid = true
    };
    private ObjectSelectorListItem<string> Website { get; } = new()
    {
        Value = ClassContentTypeType.WEBSITE,
        Text = ClassContentTypeType.WEBSITE,
        IsValid = true
    };
    private ObjectSelectorListItem<string> Email { get; } = new()
    {
        Value = ClassContentTypeType.EMAIL,
        Text = ClassContentTypeType.EMAIL,
        IsValid = true
    };
    private ObjectSelectorListItem<string> Headless { get; } = new()
    {
        Value = ClassContentTypeType.HEADLESS,
        Text = ClassContentTypeType.HEADLESS,
        IsValid = true
    };
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Inavlid", Value = "" };
    private IEnumerable<ObjectSelectorListItem<string>> items = [];

    public ContentTypeTypeGeneralSelectorDataProvider() =>
        items = [Reusable, Website, Email, Headless];

    public Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            items = items.Where(i => i.Text.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = items,
        });
    }

    public Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken) =>
        Task.FromResult((selectedValues ?? []).Select(GetSelectedItemByValue));

    private ObjectSelectorListItem<string> GetSelectedItemByValue(string contentTypeTypeValue) =>
        contentTypeTypeValue switch
        {
            ClassContentTypeType.REUSABLE => Reusable,
            ClassContentTypeType.WEBSITE => Website,
            ClassContentTypeType.EMAIL => Email,
            ClassContentTypeType.HEADLESS => Headless,
            _ => InvalidItem
        };
}

public class ContentTypeTypeWhereConditionBuilder : IWhereConditionBuilder
{
    public Task<IWhereCondition> Build(string columnName, object value)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            throw new ArgumentException(
                $"{nameof(columnName)} cannot be a null or an empty string.");
        }

        var whereCondition = new WhereCondition();

        if (value is null || value is not IEnumerable<string> contentTypeUses)
        {
            return Task.FromResult<IWhereCondition>(whereCondition);
        }

        _ = whereCondition.WhereIn(columnName, contentTypeUses.ToArray());

        return Task.FromResult<IWhereCondition>(whereCondition);
    }
}
