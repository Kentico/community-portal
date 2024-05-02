using CMS.Core;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Base controller type used for scenarios where requests could fail and need error handling
/// </summary>
/// <param name="log"></param>
public class PortalHandlerController(IEventLogService log) : Controller
{
    protected IEventLogService Log { get; } = log;

    protected Func<string, StatusCodeResult> LogAndReturnError(string eventCode) =>
        (errorMessage) =>
        {
            Log.LogError(GetType().Name, eventCode, errorMessage);

            return StatusCode(StatusCodes.Status500InternalServerError);
        };

    protected Func<string, Task<StatusCodeResult>> LogAndReturnErrorAsync(string eventCode) =>
        (errorMessage) =>
        {
            Log.LogError(GetType().Name, eventCode, errorMessage);

            return Task.FromResult(StatusCode(StatusCodes.Status500InternalServerError));
        };
}

public static class StatusCodeResultExtensions
{
    public static StatusCodeResult AsStatusCodeResult(this StatusCodeResult result) => result;
}
