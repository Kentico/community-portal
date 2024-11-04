namespace Kentico.Community.Portal.Core.Content;

public partial class AlertMessageContent
{
    public AlertMessageContentTypes AlertMessageContentTypeParsed =>
        Enum.TryParse<AlertMessageContentTypes>(AlertMessageContentType, out var type)
            ? type
            : AlertMessageContentTypes.Medium;
}

public enum AlertMessageContentTypes
{
    High,
    Medium,
    Low
}
