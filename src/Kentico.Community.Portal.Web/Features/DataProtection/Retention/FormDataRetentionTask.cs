using CMS.Base;
using CMS.Scheduler;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Features.DataProtection.Retention;
using Microsoft.Extensions.Options;

[assembly: RegisterScheduledTask(
    identifier: FormDataRetentionTask.IDENTIFIER,
    taskType: typeof(FormDataRetentionTask))]

namespace Kentico.Community.Portal.Web.Features.DataProtection.Retention;

/// <summary>
/// Deletes submissions of short-lived forms older than their retention days. History forms are
/// kept. Unclassified forms are kept (fail-safe) and logged, so someone sets their policy on the
/// form's Retention tab.
/// </summary>
public partial class FormDataRetentionTask(
    CommunityFormDataService formDataService,
    IReadOnlyModeProvider readOnlyProvider,
    IOptions<RetentionOptions> options,
    TimeProvider clock,
    ILogger<FormDataRetentionTask> logger) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.FormDataRetentionTask";

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Form data retention: {Count} submissions of form {FormName} deleted.")]
    partial void LogDeleted(int count, string formName);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Warning, Message = "Form data retention: form {FormName} has no retention policy, so its submissions are kept. Set one on the form's Retention tab.")]
    partial void LogUnclassified(string formName);

    public Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return Task.FromResult(ScheduledTaskExecutionResult.Success);
        }

        var now = clock.GetLocalNow().DateTime;

        foreach (var form in formDataService.GetForms())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (form.IsUnclassified)
            {
                LogUnclassified(form.Form.FormName);
                continue;
            }

            if (!form.IsShortLived)
            {
                continue;
            }

            int count = formDataService.DeleteItemsInsertedBefore(form, now.AddDays(-form.ShortLivedRetentionDays(options.Value)));
            if (count > 0)
            {
                LogDeleted(count, form.Form.FormName);
            }
        }

        return Task.FromResult(ScheduledTaskExecutionResult.Success);
    }
}
