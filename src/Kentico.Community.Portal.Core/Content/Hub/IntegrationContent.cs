

using System.ComponentModel;
using EnumsNET;

namespace Kentico.Community.Portal.Core.Content;

public partial class IntegrationContent
{
    public enum IntegrationType
    {
        [Description("Product Feature")]
        [CodeName("ProductFeature")]
        ProductFeature,
        [Description("Kentico Integration (7-Day Bugfix Support Policy)")]
        [CodeName("KenticoIntegration_7-DayBugfixSupportPolicy")]
        KenticoIntegrationSevenDay,
        [Description("Kentico Integration (Labs)")]
        [CodeName("KenticoIntegration_Labs")]
        KenticoIntegrationLabs,
        [Description("Partner Integration")]
        [CodeName("PartnerIntegration")]
        PartnerIntegration,
        [Description("Individual Contribution")]
        [CodeName("IndividualContribution")]
        IndividualContribution,
        [Description("Unknown")]
        [CodeName("Unknown")]
        Unknown
    }

    public IntegrationType TypeParsed =>
            Enum.TryParse<IntegrationType>(IntegrationContentType, out var value)
                ? value
                : IntegrationType.Unknown;

    public string AuthorNameNormalized =>
        TypeParsed switch
        {
            IntegrationType.ProductFeature or IntegrationType.KenticoIntegrationSevenDay or IntegrationType.KenticoIntegrationLabs => "Kentico",
            IntegrationType.Unknown => "Community",
            IntegrationType.IndividualContribution or IntegrationType.PartnerIntegration or _ => IntegrationContentAuthorName ?? "Community"
        };

    public string IntegrationTypeDisplayName
    {
        get
        {
            var member = Enums.GetMember(TypeParsed);
            if (member is null)
            {
                return Enums.GetMember(IntegrationType.Unknown)?.Name ?? "";
            }

            return member.Attributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? member.Name;
        }
    }
    public string IntegrationTypeCodeName
    {
        get
        {
            var member = Enums.GetMember(TypeParsed);
            if (member is null)
            {
                return Enums.GetMember(IntegrationType.Unknown)?.Name ?? "";
            }

            return member.Attributes.OfType<CodeNameAttribute>().FirstOrDefault()?.Name ?? member.Name;
        }
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class CodeNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
