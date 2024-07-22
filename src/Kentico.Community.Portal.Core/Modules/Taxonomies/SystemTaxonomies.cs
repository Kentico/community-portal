namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// Taxonomies required by the system
/// </summary>
public static partial class SystemTaxonomies
{
    public static BlogTypeTaxonomy BlogType { get; } = new();
    public static QAndADiscussionTypeTaxonomy QAndADiscussionType { get; } = new();
    public static IntegrationTypeTaxonomy IntegrationType { get; } = new();
    public static ContentAuthorizationTaxonomy ContentAuthorization { get; } = new();

    public static readonly IReadOnlyList<ISystemTaxonomy> ProtectedTaxonomies =
    [
        BlogType,
        QAndADiscussionType,
        IntegrationType,
        ContentAuthorization
    ];

    public static bool Includes(TaxonomyInfo taxonomy) =>
        ProtectedTaxonomies.Select(t => t.TaxonomyGUID).Contains(taxonomy.TaxonomyGUID);

    public static bool Includes(TagInfo tag) =>
        ProtectedTaxonomies.SelectMany(t => t.ProtectedTags.Select(t => t.TagGUID)).Contains(tag.TagGUID);
}
