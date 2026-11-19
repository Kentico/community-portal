using CMS.Activities;
using CMS.DataEngine;

namespace Kentico.Community.Portal.Web.Features.DataProtection.Retention;

/// <param name="Deleted">Activities deleted.</param>
/// <param name="Completed">False when cancellation stopped the delete before it ran out of rows.</param>
public record BatchDeleteResult(int Deleted, bool Completed);

/// <summary>
/// Deletes <c>OM_Activity</c> rows in bounded batches, so a large run (the first run after years
/// of page visits) isn't one DELETE in one transaction: log growth, lock escalation and blocking
/// live activity logging.
/// </summary>
/// <remarks>
/// Each batch selects the next <c>TOP n</c> matching ActivityIDs above the previous batch's highest
/// ID (keyset on the clustered primary key), then bulk deletes that ID range with the same
/// condition. Both statements seek the primary key, every batch is its own short transaction, and
/// the keyset always moves forward, so the loop ends even if a delete removes nothing.
/// </remarks>
public static class ActivityBatchDelete
{
    /// <summary>
    /// Deletes every activity matching <paramref name="where"/>, <paramref name="batchSize"/> rows
    /// per statement, checking <paramref name="cancellationToken"/> between batches.
    /// </summary>
    public static async Task<BatchDeleteResult> Run(
        IInfoProvider<ActivityInfo> activityProvider,
        WhereCondition where,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        int lastID = 0;

        return await RunBatches(async token =>
        {
            var batch = await activityProvider.Get()
                .Where(where)
                .WhereGreaterThan(nameof(ActivityInfo.ActivityID), lastID)
                .OrderBy(nameof(ActivityInfo.ActivityID))
                .TopN(batchSize)
                .Column(nameof(ActivityInfo.ActivityID))
                .GetEnumerableTypedResultAsync(cancellationToken: token);

            var ids = batch.Select(a => a.ActivityID).ToList();
            if (ids.Count == 0)
            {
                return 0;
            }

            int maxID = ids.Max();

            // A new WhereCondition, so the caller's condition isn't modified
            activityProvider.BulkDelete(new WhereCondition()
                .Where(where)
                .WhereGreaterThan(nameof(ActivityInfo.ActivityID), lastID)
                .WhereLessOrEquals(nameof(ActivityInfo.ActivityID), maxID));

            lastID = maxID;

            return ids.Count;
        }, cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="deleteBatch"/> until it returns 0 or cancellation is requested, and sums
    /// the rows it reports. Cancellation while a batch is selecting stops without losing the total.
    /// </summary>
    public static async Task<BatchDeleteResult> RunBatches(Func<CancellationToken, Task<int>> deleteBatch, CancellationToken cancellationToken)
    {
        int total = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            int deleted;
            try
            {
                deleted = await deleteBatch(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (deleted == 0)
            {
                return new BatchDeleteResult(total, Completed: true);
            }

            total += deleted;
        }

        return new BatchDeleteResult(total, Completed: false);
    }
}
