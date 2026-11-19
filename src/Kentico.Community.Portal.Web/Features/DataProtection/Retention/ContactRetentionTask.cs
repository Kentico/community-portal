using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using CMS.Scheduler;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Features.DataProtection.Retention;
using Microsoft.Extensions.Options;

[assembly: RegisterScheduledTask(
    identifier: ContactRetentionTask.IDENTIFIER,
    taskType: typeof(ContactRetentionTask))]

namespace Kentico.Community.Portal.Web.Features.DataProtection.Retention;

/// <summary>
/// Deletes inactive contacts: anonymous after
/// <see cref="RetentionOptions.AnonymousContactInactiveDays"/>, known (email) contacts that aren't
/// members or active subscribers after <see cref="RetentionOptions.KnownContactInactiveDays"/>.
/// Member and subscriber contacts are kept. Replaces Xperience's built-in inactive contact
/// deletion, which would also delete quiet subscribers and members; keep that task disabled.
/// </summary>
public partial class ContactRetentionTask(
    IContactsBulkDeletionService bulkDeletionService,
    IInfoProvider<ContactInfo> contactProvider,
    IReadOnlyModeProvider readOnlyProvider,
    IOptions<RetentionOptions> options,
    TimeProvider clock,
    ILogger<ContactRetentionTask> logger) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.ContactRetentionTask";

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Contact retention: {Anonymous} anonymous and {Known} known contacts deleted.")]
    partial void LogResult(int anonymous, int known);

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return ScheduledTaskExecutionResult.Success;
        }

        var now = clock.GetLocalNow().DateTime;
        var settings = options.Value;

        var anonymous = new WhereCondition()
            .Where(RetentionConditions.WithoutEmail())
            .Where(RetentionConditions.InactiveSince(now.AddDays(-settings.AnonymousContactInactiveDays)));

        var known = new WhereCondition()
            .Where(RetentionConditions.WithEmail())
            .Where(RetentionConditions.InactiveSince(now.AddDays(-settings.KnownContactInactiveDays)))
            .Where(RetentionConditions.NotMember())
            .Where(RetentionConditions.NotActiveSubscriber());

        int anonymousCount = await Delete(anonymous, cancellationToken);
        int knownCount = await Delete(known, cancellationToken);

        if (anonymousCount > 0 || knownCount > 0)
        {
            LogResult(anonymousCount, knownCount);
        }

        return ScheduledTaskExecutionResult.Success;
    }

    private async Task<int> Delete(WhereCondition where, CancellationToken cancellationToken)
    {
        int count = await contactProvider.Get().Where(where).GetCountAsync(cancellationToken: cancellationToken);
        if (count > 0)
        {
            await bulkDeletionService.BulkDelete(where, cancellationToken: cancellationToken);
        }

        return count;
    }
}
