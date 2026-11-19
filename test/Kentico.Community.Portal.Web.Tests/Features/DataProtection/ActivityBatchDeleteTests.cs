using CMS.Activities;
using CMS.DataEngine;
using CMS.Tests;
using Kentico.Community.Portal.Web.Features.DataProtection.Retention;

namespace Kentico.Community.Portal.Web.Tests.Features.DataProtection;

/// <summary>
/// The batching loop: runs until a batch deletes nothing, sums the batches, and stops between
/// batches when cancelled without losing the total.
/// </summary>
public class ActivityBatchDeleteTests : UnitTests
{
    [Test]
    public async Task Runs_Until_A_Batch_Deletes_Nothing_And_Sums_The_Batches()
    {
        var batches = new Queue<int>([4000, 4000, 123, 0]);
        int calls = 0;

        var result = await ActivityBatchDelete.RunBatches(_ =>
        {
            calls++;
            return Task.FromResult(batches.Dequeue());
        }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new BatchDeleteResult(8123, Completed: true)));
            Assert.That(calls, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task Completes_Without_Deleting_When_Nothing_Matches()
    {
        var result = await ActivityBatchDelete.RunBatches(_ => Task.FromResult(0), CancellationToken.None);

        Assert.That(result, Is.EqualTo(new BatchDeleteResult(0, Completed: true)));
    }

    [Test]
    public async Task Stops_Between_Batches_When_Cancelled()
    {
        using var cts = new CancellationTokenSource();
        int calls = 0;

        var result = await ActivityBatchDelete.RunBatches(_ =>
        {
            calls++;
            if (calls == 2)
            {
                cts.Cancel();
            }

            return Task.FromResult(10);
        }, cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new BatchDeleteResult(20, Completed: false)));
            Assert.That(calls, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Does_Not_Start_When_Already_Cancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        int calls = 0;

        var result = await ActivityBatchDelete.RunBatches(_ =>
        {
            calls++;
            return Task.FromResult(10);
        }, cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new BatchDeleteResult(0, Completed: false)));
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public async Task Keeps_The_Total_When_Cancelled_During_A_Batch()
    {
        using var cts = new CancellationTokenSource();
        int calls = 0;

        var result = await ActivityBatchDelete.RunBatches(token =>
        {
            calls++;
            if (calls == 3)
            {
                cts.Cancel();
                token.ThrowIfCancellationRequested();
            }

            return Task.FromResult(5);
        }, cts.Token);

        Assert.That(result, Is.EqualTo(new BatchDeleteResult(10, Completed: false)));
    }

    [Test]
    public async Task Rethrows_A_Cancellation_It_Did_Not_Request()
    {
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ActivityBatchDelete.RunBatches(_ => throw new OperationCanceledException(), CancellationToken.None));
    }

    [Test]
    public async Task Rejects_A_Batch_Size_Below_One()
    {
        var provider = Substitute.For<IInfoProvider<ActivityInfo>>();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            ActivityBatchDelete.Run(provider, new WhereCondition(), 0, CancellationToken.None));
    }
}
