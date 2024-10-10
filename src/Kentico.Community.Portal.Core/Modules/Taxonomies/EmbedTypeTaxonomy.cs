namespace Kentico.Community.Portal.Core.Modules;

public static partial class SystemTaxonomies
{
    public record EmbedTypeTaxonomy : ISystemTaxonomy
    {
        public static YouTubeTag YouTube { get; } = new();
        public static VimeoTag Vimeo { get; } = new();
        public static LinkedInTag LinkedIn { get; } = new();
        public static OtherTag Other { get; } = new();

        public static Guid GUID { get; } = new("43d45c4b-9e32-4a0b-bb51-5f9722060cf9");
        public const string CodeName = "EmbedType";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } =
        [
            YouTube,
            Vimeo,
            LinkedIn,
            Other
        ];

        public record YouTubeTag : ISystemTag
        {
            public static Guid GUID { get; } = new("48fb8642-6ce1-4e7f-9659-8849cb4a510d");
            public static string CodeName { get; } = "YouTube";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }

        public record VimeoTag : ISystemTag
        {
            public static Guid GUID { get; } = new("50dc3ef9-adee-4250-82ee-f5e517472e5b");
            public static string CodeName { get; } = "Vimeo";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }

        public record LinkedInTag : ISystemTag
        {
            public static Guid GUID { get; } = new("96e5bc08-ad27-42da-9145-cfc397bc8189");
            public static string CodeName { get; } = "LinkedIn";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }

        public record OtherTag : ISystemTag
        {
            public static Guid GUID { get; } = new("06900415-9c32-4732-a34e-2983d162d669");
            public static string CodeName { get; } = "Other";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }
    }
}
