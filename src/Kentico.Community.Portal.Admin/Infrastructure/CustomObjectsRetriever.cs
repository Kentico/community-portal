using CMS.Base;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Community.Portal.Admin.Infrastructure;

/// <summary>
/// A custom implementation of <see cref="IObjectsRetriever" /> to enable customization of
/// the presentation of those objects in various selector components.
/// 
/// This implementation was copied from the default implementation using decompilation (ILSpy)
/// </summary>
public class CustomObjectsRetriever(IObjectDisplayOptionsProvider displayOptionsProvider) : IObjectsRetriever
{
    public const string ORDERING_COLUMN_VALUE_TEMPLATE = "(CASE WHEN {0} LIKE '{1}%' THEN 0 ELSE CASE WHEN {0} LIKE '% {1}%' THEN 1 ELSE 2 END END)";
    private readonly IObjectDisplayOptionsProvider displayOptionsProvider = displayOptionsProvider;

    /// <inheritdoc/>
    public async Task<ObjectsRetrievalResult<T>> GetObjectsAsync<T>(ObjectsRetrievalOptions<T> retrievalParams, CancellationToken cancellationToken)
    {
        if (retrievalParams.ItemValueExtractor is null)
        {
            throw new InvalidOperationException($"The option {nameof(ObjectsRetrievalOptions<>.ItemValueExtractor)} is required.");
        }

        var typeInfo = GetTypeInfo(retrievalParams.ObjectType);
        var displayOptions = displayOptionsProvider.GetDisplayOptions(typeInfo);

        var query = GetObjectsQuery(typeInfo.ObjectType, retrievalParams.WhereCondition, retrievalParams.OrderBy, displayOptions);

        if (!string.IsNullOrEmpty(retrievalParams.SearchTerm))
        {
            displayOptions.ApplySearchFilter(query, retrievalParams.SearchTerm);

            string searchPriorityOrderBy = string.Format(ORDERING_COLUMN_VALUE_TEMPLATE, displayOptions.OrderByExpression, SqlHelper.EscapeLikeText(SqlHelper.EscapeQuotes(retrievalParams.SearchTerm)));
            var orderByColumns = new List<string> { searchPriorityOrderBy };
            orderByColumns.AddRange(GetOrderByColumns(retrievalParams.OrderBy, displayOptions.OrderByExpression));
            query.OrderByColumns = string.Join(",", orderByColumns);
        }

        query.Page(retrievalParams.PageIndex, retrievalParams.PageSize);
        query.GetTotalRecordsForPagedQuery = true;

        var objects = await query.GetDataContainerResultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        string identifierColumn = retrievalParams.IdentifierColumn;

        identifierColumn ??= GetIdentifyObjectByGuid(retrievalParams) ? typeInfo.GUIDColumn : typeInfo.CodeNameColumn;

        var getListItem = (IDataContainer data) => GetListItem(data, typeInfo, identifierColumn, retrievalParams.ItemValueExtractor, displayOptions);

        return new ObjectsRetrievalResult<T>
        {
            Objects = objects.Select(getListItem).ToArray(),
            NextPageAvailable = query.NextPageAvailable,
        };
    }


    /// <inheritdoc/>
    public async Task<IEnumerable<ObjectSelectorListItem<T>>> GetSelectedObjectsAsync<T>(SelectedObjectsRetrievalOptions<T> retrievalOptions, CancellationToken cancellationToken)
    {
        var identifiersList = retrievalOptions.SelectedObjectsIdentifiers.ToList();
        var typeInfo = GetTypeInfo(retrievalOptions.ObjectType);

        string identifierColumn = retrievalOptions.IdentifierColumn ?? (GetIdentifyObjectByGuid(retrievalOptions) ? typeInfo.GUIDColumn : typeInfo.CodeNameColumn);
        var displayOptions = displayOptionsProvider.GetDisplayOptions(typeInfo);

        var dataContainerCollection = await GetSelectedObjectsQuery(retrievalOptions, identifierColumn, displayOptions)
            .GetDataContainerResultAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var items = dataContainerCollection
            .OrderBy(data => identifiersList.IndexOf(data[identifierColumn].ToString()));

        return GetSelectedItems(items, typeInfo, identifierColumn, retrievalOptions.ItemValueExtractor, identifiersList, displayOptions);
    }


    private static IEnumerable<ObjectSelectorListItem<T>> GetSelectedItems<T>(IEnumerable<IDataContainer> data, ObjectTypeInfo typeInfo, string identifierColumn,
        ObjectValueExtractor<T> valueExtractor, IEnumerable<string> identifiers, ObjectDisplayOptions displayOptions)
    {
        var retrievedItems = data.ToDictionary(
            container => identifierColumn == typeInfo.GUIDColumn
                ? ValidationHelper.GetGuid(container[identifierColumn], Guid.Empty).ToString()
                : ValidationHelper.GetString(container[identifierColumn], null),
            container => GetListItem(container, typeInfo, identifierColumn, valueExtractor, displayOptions)
        );

        return identifiers.Select(id =>
            retrievedItems.TryGetValue(id, out var validListItem)
            ? validListItem
            : GetInvalidListItem(identifierColumn, id, typeInfo, valueExtractor)
        );
    }


    /// <summary>
    /// Gets an object query for the given <paramref name="objectType"/>, limited to the current site.
    /// </summary>
    /// <param name="objectType">Object type.</param>
    /// <param name="whereCondition">The where condition to be applied to the database query.</param>
    /// <param name="orderBy">The list of columns which the data should be sorted by, e.g. ["NodeLevel", "DocumentName DESC"]</param>
    /// <param name="displayOptions">Display options for the object type.</param>
    internal virtual ObjectQuery GetObjectsQuery(string objectType, WhereCondition? whereCondition, IEnumerable<string>? orderBy, ObjectDisplayOptions displayOptions)
    {
        var typeInfo = GetTypeInfo(objectType);
        var query = new ObjectQuery(objectType);

        var columns = new List<string>();
        columns.AddRange(displayOptions.DisplayColumns);
        columns.Add(typeInfo.CodeNameColumn);
        columns.Add(typeInfo.GUIDColumn);
        columns.Add(typeInfo.IDColumn);
        var definedColumns = columns.Where(column => IsDefined(column)).Distinct();

        query.Columns(definedColumns);

        if (whereCondition != null)
        {
            query.Where(whereCondition);
        }

        if (orderBy != null)
        {
            var orderByColumns = GetOrderByColumns(orderBy, displayOptions.OrderByExpression);

            if (orderByColumns.Any())
            {
                query.OrderByColumns = string.Join(",", orderByColumns);
            }
        }

        return query;
    }


    /// <summary>
    /// Gets the object query for the selected objects.
    /// </summary>
    private ObjectQuery GetSelectedObjectsQuery<T>(SelectedObjectsRetrievalOptions<T> retrievalOptions, string column, ObjectDisplayOptions displayOptions)
    {
        if (string.IsNullOrEmpty(retrievalOptions.ObjectType))
        {
            throw new InvalidOperationException("Object selector form component: The object type was not defined.");
        }

        if (retrievalOptions.ItemValueExtractor is null)
        {
            throw new InvalidOperationException($"The option {nameof(SelectedObjectsRetrievalOptions<T>.ItemValueExtractor)} is required.");
        }

        var typeInfo = GetTypeInfo(retrievalOptions.ObjectType);

        if (typeInfo == null)
        {
            throw new InvalidOperationException($"The object type '{retrievalOptions.ObjectType}' is not registered in the system.");
        }

        if (string.IsNullOrEmpty(column) || !IsDefined(column))
        {
            throw new InvalidOperationException($"The specified object type '{typeInfo.ObjectType}' does not have a '{column}' column defined.");
        }

        var query = GetObjectsQuery(retrievalOptions.ObjectType, null, null, displayOptions)
                    .WhereIn(column, retrievalOptions.SelectedObjectsIdentifiers.ToList());

        return query;
    }


    /// <summary>
    /// Returns object type info for the given object type.
    /// </summary>
    /// <param name="objectType">Object type.</param>
    /// <exception cref="InvalidOperationException">Thrown when object type info for the given <paramref name="objectType"/> is not found.</exception>
    private static ObjectTypeInfo GetTypeInfo(string objectType) => ObjectTypeManager.GetTypeInfo(objectType, exceptionIfNotFound: true);


    private static IEnumerable<string> GetOrderByColumns(IEnumerable<string> orderBy, string defaultOrderByColumn)
    {
        if (orderBy.Any())
        {
            return orderBy;
        }

        return IsDefined(defaultOrderByColumn) ? new string[] { defaultOrderByColumn } : Enumerable.Empty<string>();
    }


    private static ObjectSelectorListItem<T> GetListItem<T>(IDataContainer info, ObjectTypeInfo typeInfo, string identifierColumn, ObjectValueExtractor<T> valueExtractor, ObjectDisplayOptions displayOptions)
    {
        string displayName = displayOptions.BuildDisplayText(info);

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = ValidationHelper.GetString(info[identifierColumn], string.Empty);
        }

        return new ObjectSelectorListItem<T>
        {
            Value = valueExtractor.Invoke(info, typeInfo),
            Text = displayName,
            IsValid = true
        };
    }


    private static ObjectSelectorListItem<T> GetInvalidListItem<T>(string identifierColumn, string identifier, ObjectTypeInfo typeInfo, ObjectValueExtractor<T> valueExtractor)
    {
        DataContainer dc = new();
        dc.ColumnNames.Add(identifierColumn);
        dc.SetValue(identifierColumn, identifier);

        return new ObjectSelectorListItem<T>
        {
            Value = valueExtractor.Invoke(dc, typeInfo),
            IsValid = false,
        };
    }

    private static bool IsDefined(string columnName) => !string.IsNullOrEmpty(columnName) && !string.Equals(columnName, ObjectTypeInfo.COLUMN_NAME_UNKNOWN, StringComparison.Ordinal);

    private static bool GetIdentifyObjectByGuid<T>(SelectedObjectsRetrievalOptions<T> retrievalOptions)
    {
        var prop = retrievalOptions.GetType().GetProperty("IdentifyObjectByGuid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            return (bool)(prop.GetValue(retrievalOptions) ?? false);
        }
        return false;
    }

    private static bool GetIdentifyObjectByGuid<T>(ObjectsRetrievalOptions<T> retrievalOptions)
    {
        var prop = retrievalOptions.GetType().GetProperty("IdentifyObjectByGuid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            return (bool)(prop.GetValue(retrievalOptions) ?? false);
        }
        return false;
    }
}

/// <summary>
/// Supplies <see cref="ObjectDisplayOptions"/> based on the object context and details
/// </summary>
public interface IObjectDisplayOptionsProvider
{
    public ObjectDisplayOptions GetDisplayOptions(ObjectTypeInfo typeInfo);
}

public class ObjectDisplayOptions
{
    /// <summary>
    /// Columns to SELECT (in addition to standard ID/GUID/CodeName)
    /// </summary>
    public required string[] DisplayColumns { get; init; }

    /// <summary>
    /// SQL expression for ORDER BY (e.g., "[MemberName] + ' (' + [MemberEmail] + ')'")
    /// Already includes brackets if needed
    /// </summary>
    public required string OrderByExpression { get; init; }

    /// <summary>
    /// Function to build display text from selected data
    /// </summary>
    public required Func<IDataContainer, string> BuildDisplayText { get; init; }

    /// <summary>
    /// Function to apply search filter to query
    /// </summary>
    public required Action<ObjectQuery, string> ApplySearchFilter { get; init; }
}

public class DefaultObjectDisplayOptionsProvider : IObjectDisplayOptionsProvider
{
    // SQL: COALESCE(NULLIF(LTRIM(RTRIM(COALESCE([MemberFirstName], '') + ' ' + COALESCE([MemberLastName], ''))), ''), [MemberName]) + ' (' + [MemberEmail] + ')'
    // This replicates CommunityMember.DisplayName logic: use FirstName + LastName if not empty, otherwise fallback to UserName
    private const string MEMBER_DISPLAY_EXPRESSION = "COALESCE(NULLIF(LTRIM(RTRIM(COALESCE([MemberFirstName], '') + ' ' + COALESCE([MemberLastName], ''))), ''), [MemberName]) + ' (' + [MemberEmail] + ')'";
    private static readonly string[] member_DISPLAY_COLUMNS = ["MemberFirstName", "MemberLastName", "MemberName", "MemberEmail"];

    private const string DISCUSSION_EVENT_DISPLAY_EXPRESSION = "[DiscussionEventID] DESC";
    private static readonly string[] discussionEvent_DISPLAY_COLUMNS =
    [
        nameof(DiscussionEventInfo.DiscussionEventID),
        nameof(DiscussionEventInfo.DiscussionEventType),
        nameof(DiscussionEventInfo.DiscussionEventDateModified)
    ];

    private const string WEBPAGEITEM_DISPLAY_EXPRESSION = "[WebPageItemTreePath]";
    private static readonly string[] webPageItem_DISPLAY_COLUMNS =
    [
        nameof(WebPageItemInfo.WebPageItemName),
        nameof(WebPageItemInfo.WebPageItemTreePath)
    ];

    public ObjectDisplayOptions GetDisplayOptions(ObjectTypeInfo typeInfo)
    {
        if (string.Equals(typeInfo.ObjectClassName, MemberInfo.OBJECT_TYPE, StringComparison.OrdinalIgnoreCase))
        {
            return new ObjectDisplayOptions
            {
                DisplayColumns = member_DISPLAY_COLUMNS,
                OrderByExpression = MEMBER_DISPLAY_EXPRESSION,
                BuildDisplayText = data =>
                {
                    // Replicate CommunityMember.DisplayName logic
                    string firstName = ValidationHelper.GetString(data["MemberFirstName"], string.Empty);
                    string lastName = ValidationHelper.GetString(data["MemberLastName"], string.Empty);
                    string fullName = $"{firstName} {lastName}".Trim();
                    string displayName = string.IsNullOrWhiteSpace(fullName)
                        ? ValidationHelper.GetString(data["MemberName"], string.Empty)
                        : fullName;
                    string memberEmail = ValidationHelper.GetString(data["MemberEmail"], string.Empty);
                    return $"{displayName} ({memberEmail})";
                },
                ApplySearchFilter = (query, term) => query.Where(new WhereCondition()
                    .WhereContains("MemberFirstName", term)
                    .Or()
                    .WhereContains("MemberLastName", term)
                    .Or()
                    .WhereContains("MemberName", term)
                    .Or()
                    .WhereContains("MemberEmail", term))
            };
        }

        if (string.Equals(typeInfo.ObjectClassName, DiscussionEventInfo.OBJECT_TYPE, StringComparison.OrdinalIgnoreCase))
        {
            return new ObjectDisplayOptions
            {
                DisplayColumns = discussionEvent_DISPLAY_COLUMNS,
                OrderByExpression = DISCUSSION_EVENT_DISPLAY_EXPRESSION,
                BuildDisplayText = data =>
                {
                    // Replicate CommunityMember.DisplayName logic
                    int id = ValidationHelper.GetInteger(data["DiscussionEventID"], 0);
                    string type = ValidationHelper.GetString(data["DiscussionEventType"], string.Empty);
                    string date = ValidationHelper.GetDateTime(data["DiscussionEventDateModified"], DateTime.Now, "en-US").ToString("d");
                    return $"{id} ({type} - {date})".Trim();
                },
                ApplySearchFilter = (query, term) => query.Where(new WhereCondition()
                    .WhereContains("DiscussionEventID", term)
                    .Or()
                    .WhereContains("DiscussionEventType", term))
            };
        }

        if (string.Equals(typeInfo.ObjectClassName, WebPageItemInfo.OBJECT_TYPE, StringComparison.OrdinalIgnoreCase))
        {
            return new ObjectDisplayOptions
            {
                DisplayColumns = webPageItem_DISPLAY_COLUMNS,
                OrderByExpression = WEBPAGEITEM_DISPLAY_EXPRESSION,
                BuildDisplayText = data =>
                {
                    string name = ValidationHelper.GetString(data["WebPageItemName"], string.Empty);
                    string treePath = ValidationHelper.GetString(data["WebPageItemTreePath"], string.Empty);
                    return $"{name} ({treePath})";
                },
                ApplySearchFilter = (query, term) => query.Where(new WhereCondition()
                    .WhereContains("WebPageItemName", term)
                    .Or()
                    .WhereContains("WebPageItemTreePath", term))
            };
        }

        string[] displayColumns = typeInfo.DisplayNameColumn is not null ? [typeInfo.DisplayNameColumn] : [typeInfo.IDColumn];
        string filterColumn = displayColumns[0];


        // Default fallback for standard objects
        return new ObjectDisplayOptions
        {
            DisplayColumns = displayColumns,
            OrderByExpression = typeInfo.DisplayNameColumn is not null ? $"[{typeInfo.DisplayNameColumn}]" : $"[{typeInfo.OrderColumn}]",
            BuildDisplayText = data => data[filterColumn]?.ToString() ?? string.Empty,
            ApplySearchFilter = (query, term) => query.WhereContains(filterColumn, term)
        };
    }
}
