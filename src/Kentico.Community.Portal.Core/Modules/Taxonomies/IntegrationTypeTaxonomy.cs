namespace Kentico.Community.Portal.Core.Modules;

public static partial class SystemTaxonomies
{
    public record IntegrationTypeTaxonomy : ISystemTaxonomy
    {
        public static ProductFeatureTag ProductFeature { get; } = new();
        public static IndividualContributionTag IndividualContribution { get; } = new();
        public static KenticoIntegration_7DayBugfixSupportPolicyTag KenticoIntegration_7DayBugfixSupportPolicy { get; } = new();
        public static KenticoIntegration_LabsTag KenticoIntegration_Labs { get; } = new();
        public static PartnerIntegrationTag PartnerIntegration { get; } = new();

        public static Guid GUID { get; } = new("97cd2b53-499b-435c-a083-30a7dd510167");
        public const string CodeName = "IntegrationType";

        public Guid TaxonomyGUID => GUID;
        public string TaxonomyName => CodeName;

        public IReadOnlyList<ISystemTag> ProtectedTags { get; } =
        [
            ProductFeature,
            IndividualContribution,
            KenticoIntegration_7DayBugfixSupportPolicy,
            KenticoIntegration_Labs,
            PartnerIntegration
        ];

        public record ProductFeatureTag : ISystemTag
        {
            public static Guid GUID { get; } = new("53596b0c-b880-4d01-9acc-36b882a1d489");
            public static string CodeName { get; } = "ProductFeature";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }

        public record IndividualContributionTag : ISystemTag
        {
            public static Guid GUID { get; } = new("0483906a-d94b-4688-b4da-809098e18da4");
            public static string CodeName { get; } = "IndividualContribution";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }

        public record KenticoIntegration_7DayBugfixSupportPolicyTag : ISystemTag
        {
            public static Guid GUID { get; } = new("f7e9d489-1b9e-4ddd-a78d-9c40ee5eae6e");
            public static string CodeName { get; } = "KenticoIntegration_7-DayBugfixSupportPolicy";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }

        public record KenticoIntegration_LabsTag : ISystemTag
        {
            public static Guid GUID { get; } = new("bad5c92b-3556-4cdb-a17f-24867c6faa8e");
            public static string CodeName { get; } = "KenticoIntegration_Labs";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }

        public record PartnerIntegrationTag : ISystemTag
        {
            public static Guid GUID { get; } = new("b29e7e91-d1fa-49f0-b73c-172cb9b4d9ab");
            public static string CodeName { get; } = "PartnerIntegration";

            public Guid TagGUID => GUID;
            public string TagName => CodeName;
        }
    }
}
