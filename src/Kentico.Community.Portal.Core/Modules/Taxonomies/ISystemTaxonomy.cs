namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// Represents a well-known taxonomy required and protected by the application
/// </summary>
public interface ISystemTaxonomy
{
    public Guid TaxonomyGUID { get; }
    public string TaxonomyName { get; }

    public IReadOnlyList<ISystemTag> ProtectedTags { get; }
}

/// <summary>
/// Represents a well-known taxonomy tag required and protected by the application
/// </summary>
public interface ISystemTag
{
    public Guid TagGUID { get; }
    public string TagName { get; }
}
