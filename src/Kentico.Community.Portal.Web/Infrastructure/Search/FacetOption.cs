namespace Kentico.Community.Portal.Web.Infrastructure.Search;

public class FacetOption
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public float Count { get; set; }
    public bool IsSelected { get; set; }
}
