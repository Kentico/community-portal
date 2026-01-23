using CMS.Scheduler;
using Kentico.Community.Portal.Web.Features.QAndA.Events;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;

[assembly: RegisterScheduledTask(
    identifier: QAndADiscussionNotificationDailyOnceScheduledTask.IDENTIFIER,
    taskType: typeof(QAndADiscussionNotificationDailyOnceScheduledTask))]

namespace Kentico.Community.Portal.Web.Features.QAndA.Events;

public partial class QAndADiscussionNotificationDailyOnceScheduledTask(
    IServiceProvider serviceProvider,
    ILogger<QAndADiscussionNotificationDailyOnceScheduledTask> logger) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.QAndADiscussionNotificationDailyOnceScheduledTask";

    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<QAndADiscussionNotificationDailyOnceScheduledTask> logger = logger;

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to process daily once Q&A notifications. Error: {Error}")]
    private static partial void LogProcessingError(ILogger logger, string error);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processed daily once Q&A discussion notifications successfully")]
    private static partial void LogProcessingSuccess(ILogger logger);

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<QAndANotificationsProcessor>();

        var result = await processor.Process(QAndANotificationFrequencyType.DailyOnce);

        if (result.IsFailure)
        {
            LogProcessingError(logger, result.Error);
            return new ScheduledTaskExecutionResult(result.Error);
        }

        LogProcessingSuccess(logger);
        return ScheduledTaskExecutionResult.Success;
    }
}
