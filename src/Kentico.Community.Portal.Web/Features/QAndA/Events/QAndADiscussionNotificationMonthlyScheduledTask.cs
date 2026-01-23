using CMS.Scheduler;
using Kentico.Community.Portal.Web.Features.QAndA.Events;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;

[assembly: RegisterScheduledTask(
    identifier: QAndADiscussionNotificationMonthlyScheduledTask.IDENTIFIER,
    taskType: typeof(QAndADiscussionNotificationMonthlyScheduledTask))]

namespace Kentico.Community.Portal.Web.Features.QAndA.Events;

public partial class QAndADiscussionNotificationMonthlyScheduledTask(
    IServiceProvider serviceProvider,
    ILogger<QAndADiscussionNotificationMonthlyScheduledTask> logger) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.QAndADiscussionNotificationMonthlyScheduledTask";

    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<QAndADiscussionNotificationMonthlyScheduledTask> logger = logger;

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to process monthly Q&A notifications. Error: {Error}")]
    private static partial void LogProcessingError(ILogger logger, string error);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processed monthly Q&A discussion notifications successfully")]
    private static partial void LogProcessingSuccess(ILogger logger);

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<QAndANotificationsProcessor>();

        var result = await processor.Process(QAndANotificationFrequencyType.Monthly);

        if (result.IsFailure)
        {
            LogProcessingError(logger, result.Error);
            return new ScheduledTaskExecutionResult(result.Error);
        }

        LogProcessingSuccess(logger);
        return ScheduledTaskExecutionResult.Success;
    }
}
