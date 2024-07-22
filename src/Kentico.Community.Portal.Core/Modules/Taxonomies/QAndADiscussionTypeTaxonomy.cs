namespace Kentico.Community.Portal.Core.Modules;

public static partial class SystemTaxonomies
{
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
}
