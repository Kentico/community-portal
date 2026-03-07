namespace Kentico.Community.Portal.Core.Content;

public partial class AlertMessageContent
{
    public static Guid CONTENT_TYPE_GUID { get; } = new Guid("fea72523-73cf-4d4f-b37e-e0d960b7621f");

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
