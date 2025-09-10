namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Error response model for API and HTMX error responses
/// </summary>
public class ErrorResponse
{
    public ErrorDetail Detail { get; set; } = new();
}

/// <summary>
/// Error detail information
/// </summary>
public class ErrorDetail
{
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "failure";
}
