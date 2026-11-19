namespace Kentico.Community.Portal.Core.Content;

public record WebPageMeta
{
    public static WebPageMeta Default { get; } = new("", "");

    /// <summary>
    /// Robots value for routes that hold no answer: kept out of the index, links still followed.
    /// </summary>
    public const string ROBOTS_NOINDEX = "noindex, follow";

    public WebPageMeta(IWebPageMetaFields meta)
    {
        Title = Maybe
            .From(meta.WebPageMetaTitle)
            .MapNullOrWhiteSpaceAsNone()
            .IfNoValue(meta is IBasicItemFields basicItem ? basicItem.BasicItemTitle : Maybe<string>.None)
            .GetValueOrDefault("");

        Description = Maybe
            .From(meta.WebPageMetaShortDescription)
            .MapNullOrWhiteSpaceAsNone()
            .IfNoValue(meta is IBasicItemFields basicItem2 ? basicItem2.BasicItemShortDescription : Maybe<string>.None)
            .GetValueOrDefault("");

        Robots = meta.WebPageMetaRobots;
        CanonicalURL = Maybe.From(meta.WebPageCanonicalURL).MapNullOrWhiteSpaceAsNone();

        // Editor-selected featured image, site-relative until resolved against the request
        OGImageURL = meta is IFeaturedImageFields featured
            ? Maybe.From(featured.FeaturedImageImageContent?.FirstOrDefault()?.ImageContentAsset?.Url).MapNullOrWhiteSpaceAsNone()
            : Maybe<string>.None;
        OGType = meta is BlogPostPage or QAndAQuestionPage ? OG_TYPE_ARTICLE : OG_TYPE_WEBSITE;
    }

    public WebPageMeta(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; init; }
    public string Description { get; init; }

    /// <summary>
    /// Absolute or site-relative URL. When empty, the canonical URL of the current request is used.
    /// </summary>
    public Maybe<string> CanonicalURL { get; init; }
    /// <summary>
    /// Absolute or site-relative URL. When empty, the site logo is used.
    /// </summary>
    public Maybe<string> OGImageURL { get; init; }
    /// <summary>
    /// True when <see cref="OGImageURL"/> is the site-wide fallback rather than an image about the page.
    /// </summary>
    public bool OGImageIsFallback { get; init; }

    public const string OG_TYPE_WEBSITE = "website";
    public const string OG_TYPE_ARTICLE = "article";
    public const string OG_TYPE_PROFILE = "profile";

    /// <summary>
    /// Open Graph object type (https://ogp.me/#types)
    /// </summary>
    public string OGType { get; init; } = OG_TYPE_WEBSITE;
    /// <summary>
    /// Publication and author metadata, for pages with an <see cref="OG_TYPE_ARTICLE"/> type
    /// </summary>
    public Maybe<WebPageArticleMeta> Article { get; init; }
    /// <summary>
    /// Content of the robots meta tag. No tag is rendered when empty.
    /// </summary>
    public Maybe<string> Robots { get; set; }

    /// <summary>
    /// Fills an empty <see cref="Description"/>, leaving an editor-set value untouched.
    /// </summary>
    public WebPageMeta WithDescriptionFallback(string description) =>
        string.IsNullOrWhiteSpace(Description)
            ? this with { Description = description }
            : this;

    /// <summary>
    /// Fills an empty <see cref="Robots"/>, leaving an editor-set value untouched.
    /// </summary>
    public WebPageMeta WithRobotsFallback(string robots) =>
        Robots.MapNullOrWhiteSpaceAsNone().HasNoValue
            ? this with { Robots = robots }
            : this;
};

/// <param name="PublishedTime">First publication date</param>
/// <param name="ModifiedTime">Last publication date, when known</param>
/// <param name="AuthorName">Display name of the author</param>
/// <param name="AuthorURL">Absolute URL of the author's profile, when one exists</param>
public record WebPageArticleMeta(
    DateTime PublishedTime,
    Maybe<DateTime> ModifiedTime,
    string AuthorName,
    Maybe<string> AuthorURL);
