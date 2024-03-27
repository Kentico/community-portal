namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// Taxonomies required by the system
/// </summary>
public static class SystemTaxonomies
{
    public static BlogTypeTaxonomy BlogType { get; } = new();
    public static QAndADiscussionTypeTaxonomy QAndADiscussionType { get; } = new();

    public static readonly IReadOnlyList<ISystemTaxonomy> ProtectedTaxonomies =
    [
        BlogType,
        QAndADiscussionType
    ];

    public static bool Includes(TaxonomyInfo taxonomy) =>
        ProtectedTaxonomies.Select(t => t.TaxonomyGUID).Contains(taxonomy.TaxonomyGUID);

    public static bool Includes(TagInfo tag) =>
        ProtectedTaxonomies.SelectMany(t => t.ProtectedTags.Select(t => t.TagGUID)).Contains(tag.TagGUID);

    public record BlogTypeTaxonomy : ISystemTaxonomy
    {
        public static Guid GUID { get; } = new("8419874e-3ec4-4da4-8a32-263f7ba5b864");
        public const string CodeName = "BlogType";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } = [];
    }
    public record QAndADiscussionTypeTaxonomy : ISystemTaxonomy
    {
        public static QuestionTag Question { get; } = new();
        public static BlogTag Blog { get; } = new();

        public static Guid GUID { get; } = new("0b38791a-e864-492b-b245-a6b3f3fea46c");
        public const string CodeName = "QAndADiscussionType";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } =
        [
            Question,
            Blog
        ];

        public record QuestionTag : ISystemTag
        {
            public static Guid GUID { get; } = new("c50e7dd3-2b8e-47b5-96ee-3f04ccfde8b6");
            public static string CodeName { get; } = "Question";

            public Guid TagGUID => GUID;

            public string TagName => CodeName;
        }

        public record BlogTag : ISystemTag
        {
            public static Guid GUID { get; } = new("0a81201d-8daa-4a54-bcc1-320914635b8f");
            public static string CodeName { get; } = "Blog";

            public Guid TagGUID => GUID;

            public string TagName => CodeName;
        }
    }
    public record DXTopicTaxonomy : ISystemTaxonomy
    {
        public static Guid GUID { get; } = new("72f39193-9dee-45df-9138-730ed4445858");
        public const string CodeName = "DXTopic";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } = [];
    }
}

public interface ISystemTaxonomy
{
    Guid TaxonomyGUID { get; }
    string TaxonomyName { get; }

    IReadOnlyList<ISystemTag> ProtectedTags { get; }
}
public interface ISystemTag
{
    Guid TagGUID { get; }
    string TagName { get; }
}
