namespace Kentico.Community.Portal.Core.Content;

public record WebpageMeta(string Title, string Description)
{
    public WebpageMeta(IWebPageMetaFields meta) : this(meta.WebPageMetaTitle, meta.WebPageMetaShortDescription) => Robots = meta.WebPageMetaRobots;

    public string? CanonicalURL { get; init; }
    public string? OGImageURL { get; init; }
    public string? Robots { get; set; } = null;
};
