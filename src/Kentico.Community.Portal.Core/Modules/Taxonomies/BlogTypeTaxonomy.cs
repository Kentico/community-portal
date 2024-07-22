namespace Kentico.Community.Portal.Core.Modules;

public static partial class SystemTaxonomies
{
    public record BlogTypeTaxonomy : ISystemTaxonomy
    {
        public static Guid GUID { get; } = new("8419874e-3ec4-4da4-8a32-263f7ba5b864");
        public const string CodeName = "BlogType";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } = [];
    }
}
