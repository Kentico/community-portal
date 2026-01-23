using CMS.Scheduler;
using Kentico.Community.Portal.Web.Features.QAndA.Events;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;

[assembly: RegisterScheduledTask(
    identifier: QAndADiscussionNotificationDailyTwiceScheduledTask.IDENTIFIER,
    taskType: typeof(QAndADiscussionNotificationDailyTwiceScheduledTask))]

namespace Kentico.Community.Portal.Web.Features.QAndA.Events;

public partial class QAndADiscussionNotificationDailyTwiceScheduledTask(
    IServiceProvider serviceProvider,
    ILogger<QAndADiscussionNotificationDailyTwiceScheduledTask> logger) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.QAndADiscussionNotificationDailyTwiceScheduledTask";

    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<QAndADiscussionNotificationDailyTwiceScheduledTask> logger = logger;

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to process daily twice Q&A notifications. Error: {Error}")]
    private static partial void LogProcessingError(ILogger logger, string error);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processed daily twice Q&A discussion notifications successfully")]
    private static partial void LogProcessingSuccess(ILogger logger);

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<QAndANotificationsProcessor>();

        var result = await processor.Process(QAndANotificationFrequencyType.DailyTwice);

        if (result.IsFailure)
        {
            LogProcessingError(logger, result.Error);
            return new ScheduledTaskExecutionResult(result.Error);
        }

        LogProcessingSuccess(logger);
        return ScheduledTaskExecutionResult.Success;
    }
}
