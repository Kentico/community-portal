namespace Kentico.Community.Portal.Core.Content;

public partial class LinkContent
{
    public LinkContentLinkTypes LinkContentLinkTypeParsed =>
        Enum.TryParse<LinkContentLinkTypes>(LinkContentLinkType, out var type)
            ? type
            : LinkContentLinkTypes.PortalResource;
}

public enum LinkContentLinkTypes
{
    CommunityContribution,
    PortalResource,
    IndustryResource
}
