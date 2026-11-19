namespace Kentico.Community.Portal.Core.Modules;

public static partial class SystemTaxonomies
{
    public record MemberAssetTypeTaxonomy : ISystemTaxonomy
    {
        public static AvatarImageTag AvatarImage { get; } = new();
        public static DiscussionImageTag DiscussionImage { get; } = new();

        public static Guid GUID { get; } = new("41d8ebef-43c7-44c7-abed-ae968ff222ae");
        public const string CodeName = "MemberAssetType";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } =
        [
            AvatarImage,
            DiscussionImage
        ];

        public record AvatarImageTag : ISystemTag
        {
            public static Guid GUID { get; } = new("34c8fc6a-a7cc-465a-a873-6520f764a534");
            public static string CodeName { get; } = "AvatarImage";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }

        public record DiscussionImageTag : ISystemTag
        {
            public static Guid GUID { get; } = new("4fc6a7ca-451d-4709-a0c8-a02e40c064f7");
            public static string CodeName { get; } = "DiscussionImage";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }
    }
}
