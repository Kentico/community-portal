namespace Kentico.Community.Portal.Core.Modules;

public static partial class SystemTaxonomies
{
    public record DXTopicTaxonomy : ISystemTaxonomy
    {
        public static Guid GUID { get; } = new("72f39193-9dee-45df-9138-730ed4445858");
        public const string CodeName = "DXTopic";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } = [];
    }
}
