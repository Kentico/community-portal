namespace Kentico.Community.Portal.Core.Modules;

public static partial class SystemTaxonomies
{
    public record ContentAuthorizationTaxonomy : ISystemTaxonomy
    {
        public static CommunityLeaderTag CommunityLeader { get; } = new();
        public static InternalEmployeeTag InternalEmployee { get; } = new();
        public static MVPTag MVP { get; } = new();
        public static CommunityMemberTag Member { get; } = new();

        public static Guid GUID { get; } = new("ca9aa59d-d3fd-4f4c-bb67-5a1b3c0f5ad5");
        public const string CodeName = "ContentAuthorization";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } =
        [
            CommunityLeader,
            InternalEmployee,
            MVP,
            Member
        ];

        public record CommunityLeaderTag : ISystemTag
        {
            public static Guid GUID { get; } = new("63060a51-9025-46a4-a6ac-9a48fd712756");
            public static string CodeName { get; } = "CommunityLeader";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }
        public record InternalEmployeeTag : ISystemTag
        {
            public static Guid GUID { get; } = new("c265fa94-4982-415e-b56d-261b4f4b5b15");
            public static string CodeName { get; } = "InternalEmployee";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }
        public record MVPTag : ISystemTag
        {
            public static Guid GUID { get; } = new("83b28847-6c4d-40d1-9d76-4214c740c3c3");
            public static string CodeName { get; } = "MVP";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }
        public record CommunityMemberTag : ISystemTag
        {
            public static Guid GUID { get; } = new("a4f6a51e-27b5-4f0d-8f96-2c3519e0b1d0");
            public static string CodeName { get; } = "CommunityMember";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }
    }
}
