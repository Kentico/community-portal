namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// Represents a well-known taxonomy required and protected by the application
/// </summary>
public interface ISystemTaxonomy
{
    Guid TaxonomyGUID { get; }
    string TaxonomyName { get; }

    IReadOnlyList<ISystemTag> ProtectedTags { get; }
}

/// <summary>
/// Represents a well-known taxonomy tag required and protected by the application
/// </summary>
public interface ISystemTag
{
    Guid TagGUID { get; }
    string TagName { get; }
}
