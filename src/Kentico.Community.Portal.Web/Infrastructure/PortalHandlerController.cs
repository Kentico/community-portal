namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Generic base controller used for scenarios where requests could fail and need error handling.
/// Provides helpers to log errors with structured messages and return 500 results.
/// </summary>
public class PortalHandlerController<TController>(ILogger<TController> logger) : Controller where TController : Controller
{
    protected ILogger<TController> Logger { get; } = logger;

    protected Func<string, StatusCodeResult> LogAndReturnError(string eventCode) =>
        errorMessage =>
        {
            Logger.LogError(new EventId(0, eventCode), "Request failed: {ErrorMessage}", errorMessage);
            return StatusCode(StatusCodes.Status500InternalServerError);
        };

    protected Func<string, Task<StatusCodeResult>> LogAndReturnErrorAsync(string eventCode) =>
        errorMessage =>
        {
            Logger.LogError(new EventId(0, eventCode), "Request failed: {ErrorMessage}", errorMessage);
            return Task.FromResult(StatusCode(StatusCodes.Status500InternalServerError));
        };
}

public static class StatusCodeResultExtensions
{
    public static StatusCodeResult AsStatusCodeResult(this StatusCodeResult result) => result;
}
