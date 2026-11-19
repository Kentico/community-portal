using System.Reflection;
using CMS.DataEngine;
using CMS.Scheduler;

namespace Kentico.Community.Portal.Web.Features.Testing;

/// <summary>
/// Runs a scheduled task inline, so a spec can assert on its effects right after the call instead
/// of waiting for the scheduler's next tick. The task's configuration (enabled, interval, next run)
/// is not touched.
/// </summary>
public class E2EScheduledTaskRunner(
    IServiceProvider serviceProvider,
    IInfoProvider<ScheduledTaskConfigurationInfo> taskConfigurationProvider)
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly IInfoProvider<ScheduledTaskConfigurationInfo> taskConfigurationProvider = taskConfigurationProvider;

    /// <param name="codeName">
    /// The task configuration code name. Falls back to the registered task identifier, so a task can
    /// run before its configuration has been created and serialized.
    /// </param>
    public async Task<Result<E2ERunTaskResponse>> Run(string codeName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeName);

        var configuration = (await taskConfigurationProvider.Get()
            .WhereEquals(nameof(ScheduledTaskConfigurationInfo.ScheduledTaskConfigurationName), codeName)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault()
            ?? new ScheduledTaskConfigurationInfo
            {
                ScheduledTaskConfigurationName = codeName,
                ScheduledTaskConfigurationScheduledTaskIdentifier = codeName,
            };

        string identifier = configuration.ScheduledTaskConfigurationScheduledTaskIdentifier;

        var taskType = FindRegisteredTaskType(identifier);
        if (taskType is null)
        {
            return Result.Failure<E2ERunTaskResponse>($"No scheduled task configuration or registered task matches '{codeName}'.");
        }

        using var scope = serviceProvider.CreateScope();
        var task = (IScheduledTask)ActivatorUtilities.CreateInstance(scope.ServiceProvider, taskType);

        var result = await task.Execute(configuration, cancellationToken);
        string? message = GetResultMessage(result);

        return new E2ERunTaskResponse(codeName, identifier, string.IsNullOrEmpty(message), message);
    }

    private static Type? FindRegisteredTaskType(string identifier) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a => a.GetCustomAttributes<RegisterScheduledTaskAttribute>())
            .FirstOrDefault(a => string.Equals(a.Identifier, identifier, StringComparison.OrdinalIgnoreCase))
            ?.TaskType;

    /// <summary>
    /// <see cref="ScheduledTaskExecutionResult" /> keeps its message in an internal property; an
    /// empty message is <see cref="ScheduledTaskExecutionResult.Success" />. Read by reflection
    /// because this is test-only code and the message is what a failing spec needs to show.
    /// </summary>
    private static string? GetResultMessage(ScheduledTaskExecutionResult result) =>
        typeof(ScheduledTaskExecutionResult)
            .GetProperty("Result", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(result) as string;
}

public record E2ERunTaskRequest(string CodeName);

public record E2ERunTaskResponse(string CodeName, string Identifier, bool Succeeded, string? Message);
