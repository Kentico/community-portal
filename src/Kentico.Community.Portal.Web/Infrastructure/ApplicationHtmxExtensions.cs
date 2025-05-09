namespace Htmx;

public static class ApplicationHtmxExtensions
{
    public static HtmxResponseHeaders WithToastFailure(this HtmxResponseHeaders headers, string message) =>
        headers.Reswap("none").WithToast(message, ToastStatus.Failure);

    public static HtmxResponseHeaders WithToastSuccess(this HtmxResponseHeaders headers, string message) =>
        headers.WithToast(message, ToastStatus.Success);

    public static HtmxResponseHeaders WithToast(this HtmxResponseHeaders headers, string message, ToastStatus toastStatus)
    {
        string status = toastStatus switch
        {
            ToastStatus.Success => "success",
            ToastStatus.Failure => "failure",
            _ => ""
        };

        return headers.WithTrigger("showToast", new { status, message });
    }
}

public enum ToastStatus
{
    Success,
    Failure
}
