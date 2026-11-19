using CMS.Core;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.Membership;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.EmailQueue;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(EmailQueueListExtender))]

namespace Kentico.Community.Portal.Admin.Features.EmailQueue;

public class EmailQueueListExtender(
    IInfoProvider<EmailInfo> emailProvider,
    IEventLogService eventLogService) : PageExtender<EmailQueueList>
{
    public const string DELETE_FAILED_COMMAND = nameof(DeleteFailed);

    /// <summary>
    /// Listing template command that reloads the table data (same as built-in WebFarmTasks.ClearTaskList)
    /// </summary>
    private const string LOAD_DATA_COMMAND = "LoadData";

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var config = Page.PageConfiguration;

        // Built-in column is sortable, without a default direction
        config.ColumnConfigurations
            .TryFirst(c => string.Equals(c.Name, nameof(EmailInfo.EmailCreated), StringComparison.OrdinalIgnoreCase))
            .Tap(c => c.Sorting = new SortingConfiguration
            {
                Sortable = true,
                DefaultDirection = SortTypeEnum.Desc
            });

        _ = config.HeaderActions.AddCommandWithConfirmation(
            label: "Delete failed",
            command: DELETE_FAILED_COMMAND,
            confirmation: "Delete failed emails?",
            confirmationButton: "Delete",
            confirmationDetail: "All emails with the Failed status are permanently deleted from the queue.",
            icon: Icons.Bin,
            destructive: true);
    }

    /// <summary>
    /// Deletes all failed emails. Failed matches the built-in status column:
    /// <see cref="EmailStatusEnum.Waiting"/> with a last send result.
    /// </summary>
    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse> DeleteFailed(CancellationToken cancellationToken = default)
    {
        int deleted = 0;

        try
        {
            var failed = (await emailProvider.Get()
                .WhereEquals(nameof(EmailInfo.EmailStatus), (int)EmailStatusEnum.Waiting)
                .WhereNotNull(nameof(EmailInfo.EmailLastSendResult))
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                .ToList();

            // Per item, so attachment bindings and other dependencies are cleaned up
            foreach (var email in failed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await emailProvider.DeleteAsync(email);
                deleted++;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            eventLogService.LogException(nameof(EmailQueueListExtender), "DELETEFAILED", ex);

            return Response()
                .UseCommand(LOAD_DATA_COMMAND)
                .AddErrorMessage($"Deleting failed emails did not finish. Deleted {deleted} before the error. See the event log.");
        }

        if (deleted == 0)
        {
            return Response().AddInfoMessage("No failed emails to delete.");
        }

        return Response()
            .UseCommand(LOAD_DATA_COMMAND)
            .AddSuccessMessage($"Deleted {deleted} failed email{(deleted == 1 ? "" : "s")}.");
    }
}
