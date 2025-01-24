namespace Kentico.Community.Portal.Core.Content;

public class LinkDataType
{
    public const string FIELD_TYPE = "link";
    public const string FIELD_TYPE_LIST = "linklist";

    public Guid ID { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = "";
    public string URL { get; set; } = "";

    public bool HasValue => !(string.IsNullOrWhiteSpace(Label) || string.IsNullOrWhiteSpace(URL));
}
