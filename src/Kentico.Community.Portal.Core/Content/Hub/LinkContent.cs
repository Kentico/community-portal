namespace Kentico.Community.Portal.Core.Content;

public partial class LinkContent
{
    public static Guid CONTENT_TYPE_GUID { get; } = new Guid("05bcb412-2000-455b-95fd-74dad14fbdae");

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
