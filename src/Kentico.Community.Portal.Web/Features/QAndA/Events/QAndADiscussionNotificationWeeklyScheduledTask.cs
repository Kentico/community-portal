using CMS.Scheduler;
using Kentico.Community.Portal.Web.Features.QAndA.Events;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;

[assembly: RegisterScheduledTask(
    identifier: QAndADiscussionNotificationWeeklyScheduledTask.IDENTIFIER,
    taskType: typeof(QAndADiscussionNotificationWeeklyScheduledTask))]

namespace Kentico.Community.Portal.Web.Features.QAndA.Events;

public partial class QAndADiscussionNotificationWeeklyScheduledTask(
    IServiceProvider serviceProvider,
    ILogger<QAndADiscussionNotificationWeeklyScheduledTask> logger) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.QAndADiscussionNotificationWeeklyScheduledTask";

    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<QAndADiscussionNotificationWeeklyScheduledTask> logger = logger;

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to process weekly Q&A notifications. Error: {Error}")]
    private static partial void LogProcessingError(ILogger logger, string error);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processed weekly Q&A discussion notifications successfully")]
    private static partial void LogProcessingSuccess(ILogger logger);

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<QAndANotificationsProcessor>();

        var result = await processor.Process(QAndANotificationFrequencyType.Weekly);

        if (result.IsFailure)
        {
            LogProcessingError(logger, result.Error);
            return new ScheduledTaskExecutionResult(result.Error);
        }

        LogProcessingSuccess(logger);
        return ScheduledTaskExecutionResult.Success;
    }
}
