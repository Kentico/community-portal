namespace Kentico.Community.Portal.Web.Infrastructure.Search;

public class FacetOption
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
}

public class FacetGroup
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public int Count { get; set; }
    public IReadOnlyList<FacetOption> Facets { get; set; } = [];
}
