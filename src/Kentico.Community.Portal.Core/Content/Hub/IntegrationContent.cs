namespace Kentico.Community.Portal.Core.Content;

public partial class IntegrationContent
{
    public enum IntegrationType
    {
        ProductFeature,
        KenticoIntegrationSevenDay,
        KenticoIntegrationLabs,
        PartnerIntegration,
        IndividualContribution,
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
}
