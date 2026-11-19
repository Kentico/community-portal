using CMS.Activities;
using CMS.Base;
using CMS.DataEngine;
using CMS.Scheduler;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Features.DataProtection.Retention;
using Microsoft.Extensions.Options;

[assembly: RegisterScheduledTask(
    identifier: ActivityRetentionTask.IDENTIFIER,
    taskType: typeof(ActivityRetentionTask))]

namespace Kentico.Community.Portal.Web.Features.DataProtection.Retention;

/// <summary>
/// Deletes activities older than <see cref="RetentionOptions.ActivityDays"/>, except
/// <see cref="RetentionOptions.KeptActivityTypes"/> (member registration, form submission), in
/// batches of <see cref="RetentionOptions.ActivityDeleteBatchSize"/>.
/// </summary>
public partial class ActivityRetentionTask(
    IInfoProvider<ActivityInfo> activityProvider,
    IReadOnlyModeProvider readOnlyProvider,
    IOptions<RetentionOptions> options,
    TimeProvider clock,
    ILogger<ActivityRetentionTask> logger) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.ActivityRetentionTask";

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Activity retention: {Count} activities deleted.")]
    partial void LogResult(int count);

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return ScheduledTaskExecutionResult.Success;
        }

        var settings = options.Value;
        var where = new WhereCondition()
            .WhereLessThan(nameof(ActivityInfo.ActivityCreated), clock.GetLocalNow().DateTime.AddDays(-settings.ActivityDays));

        if (settings.KeptActivityTypes.Count > 0)
        {
            where = where.WhereNotIn(nameof(ActivityInfo.ActivityType), settings.KeptActivityTypes);
        }

        var result = await ActivityBatchDelete.Run(activityProvider, where, settings.ActivityDeleteBatchSize, cancellationToken);
        if (result.Deleted > 0)
        {
            LogResult(result.Deleted);
        }

        return result.Completed
            ? ScheduledTaskExecutionResult.Success
            : new ScheduledTaskExecutionResult($"Cancelled after deleting {result.Deleted} activities. The next run continues.");
    }
}
