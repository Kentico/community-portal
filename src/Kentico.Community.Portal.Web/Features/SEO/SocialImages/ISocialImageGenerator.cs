namespace Kentico.Community.Portal.Web.Features.SEO.SocialImages;

/// <summary>
/// Composes a branded Open Graph / Twitter Card image for a page
/// </summary>
public interface ISocialImageGenerator
{
    /// <summary>
    /// Renders <paramref name="request"/> as a <see cref="SocialImage.WIDTH"/> x <see cref="SocialImage.HEIGHT"/> PNG
    /// </summary>
    Result<byte[]> Generate(SocialImageRequest request);
}

/// <param name="Title">Main text, wrapped and shrunk to fit</param>
/// <param name="Byline">Secondary line (e.g. author), omitted when empty</param>
public record SocialImageRequest(string Title, Maybe<string> Byline);

public static class SocialImage
{
    /// <summary>
    /// Recommended Open Graph image size (1.91:1)
    /// </summary>
    public const int WIDTH = 1200;
    public const int HEIGHT = 630;
    public const string CONTENT_TYPE = "image/png";
    public const string EXTENSION = ".png";
}
