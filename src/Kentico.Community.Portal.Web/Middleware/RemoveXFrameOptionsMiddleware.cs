namespace Kentico.Community.Portal.Web.Middleware;

/// <summary>
/// Middleware that removes the X-Frame-Options header to allow the application to be used as an iframe source
/// Used for VS Code Simple Browser in local development
/// </summary>
public class RemoveXFrameOptionsMiddleware
{
    private readonly RequestDelegate next;

    public RemoveXFrameOptionsMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove("X-Frame-Options");
            return Task.CompletedTask;
        });

        await next(context);
    }
}
