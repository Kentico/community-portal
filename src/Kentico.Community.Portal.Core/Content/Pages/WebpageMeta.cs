namespace Kentico.Community.Portal.Core.Content;

public record WebPageMeta
{
    public static WebPageMeta Default { get; } = new("", "");

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
    }

    public WebPageMeta(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; init; }
    public string Description { get; init; }

    public Maybe<string> CanonicalURL { get; init; }
    public Maybe<string> OGImageURL { get; init; }
    public Maybe<string> Robots { get; set; }
};
