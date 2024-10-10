using System.Text.RegularExpressions;
using CMS.AutomationEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CSharpFunctionalExtensions;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Http;

namespace Kentico.Community.Portal.Admin.UIPages;

public class AutomationStepListFilter
{
    [GeneralSelectorComponent(
        dataProviderType: typeof(AutomationStepGeneralSelectorDataProvider),
        Label = "Steps",
        Placeholder = "Any",
        Order = 1
    )]
    [FilterCondition(
        BuilderType = typeof(GeneralWhereInWhereConditionBuilder),
        ColumnName = nameof(WorkflowStepInfo.StepName)
    )]
    public IEnumerable<string> AutomationSteps { get; set; } = [];
}

public class AutomationStepGeneralSelectorDataProvider(
    IInfoProvider<WorkflowStepInfo> stepProvider,
    IHttpContextAccessor contextAccessor,
    IConversionService conversion)
    : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<WorkflowStepInfo> stepProvider = stepProvider;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly IConversionService conversion = conversion;

    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Inavlid", Value = "" };

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var items = await stepProvider
            .Get()
            .WhereEquals(nameof(WorkflowStepInfo.StepWorkflowID), GetWorkflowID())
            .GetEnumerableTypedResultAsync();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            items = items.Where(i => i.StepDisplayName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = items.Select(i => new ObjectSelectorListItem<string> { IsValid = true, Text = i.StepDisplayName, Value = i.StepName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken)
    {
        var items = await stepProvider.Get()
            .WhereEquals(nameof(WorkflowStepInfo.StepWorkflowID), GetWorkflowID())
            .GetEnumerableTypedResultAsync();

        return (selectedValues ?? []).Select(GetSelectedItemByValue(items));

    }

    private static Func<string, ObjectSelectorListItem<string>> GetSelectedItemByValue(IEnumerable<WorkflowStepInfo> steps) =>
        (string stepName) => steps
            .TryFirst(c => string.Equals(c.StepName, stepName))
            .Map(c => new ObjectSelectorListItem<string> { IsValid = true, Text = c.StepDisplayName, Value = c.StepName })
            .GetValueOrDefault(InvalidItem);

    /// <summary>
    /// Returns the <see cref="WorkflowInfo.WorkflowID"/> of the currently viewed/edited workflow
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private int GetWorkflowID()
    {
        // Don't do this! There's no easy way to get this contextual data in a data provider
        // So for now, we rely on the HttpContext
        var pathVals = contextAccessor.HttpContext!.Request.Form["path"];
        if (!pathVals.TryFirst().TryGetValue(out string? path))
        {
            throw new Exception("This data provider can only be used in the Automation Process Contacts Tab");
        }
        string pattern = @"automation\/list\/(\d+)\/automation-contacts";
        var match = Regex.Match(path, pattern);
        if (!match.Success)
        {
            throw new Exception("This data provider can only be used in the Automation Process Contacts Tab");
        }

        return conversion.GetInteger(match.Groups[1].Value, 0);
    }
}

