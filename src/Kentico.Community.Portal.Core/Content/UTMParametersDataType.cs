namespace Kentico.Community.Portal.Core.Content;

public class UTMParametersDataType
{
    public const string FIELD_TYPE = "utmparameters";

    public Guid ID { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = "";
    public string Medium { get; set; } = "";
    public string Campaign { get; set; } = "";
    public string Content { get; set; } = "";
    public string Term { get; set; } = "";
}
