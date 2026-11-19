using ImageMagick;
using ImageMagick.Drawing;
using Microsoft.Extensions.FileProviders;

namespace Kentico.Community.Portal.Web.Features.SEO.SocialImages;

/// <summary>
/// Composes social images with ImageMagick (Magick.NET, already in the Xperience package graph)
/// using the brand fonts shipped in the site's static assets
/// </summary>
/// <param name="webRoot">Site static assets (<c>wwwroot</c>), the source of the brand fonts and logo</param>
public class MagickSocialImageGenerator(IFileProvider webRoot) : ISocialImageGenerator
{
    public const string FONT_TITLE = "fonts/GTWalsheimPro-Bold.ttf";
    public const string FONT_LABEL = "fonts/GTWalsheimPro-Medium.ttf";
    public const string FONT_BYLINE = "fonts/GTWalsheimPro-Regular.ttf";

    /// <summary>
    /// Kentico logo (orange mark, wordmark recolored to white for the purple background)
    /// </summary>
    public const string LOGO = "img/logo.svg";
    public const string SITE_LABEL = "Community";
    public const int LOGO_HEIGHT = 96;

    /// <summary>
    /// Brand <c>$purple</c> (Client/styles/00_core/_variables.scss)
    /// </summary>
    public static readonly MagickColor Background = new("#7f09b7");
    /// <summary>
    /// Brand <c>$orange</c> (Client/styles/00_core/_variables.scss)
    /// </summary>
    public static readonly MagickColor Accent = new("#f05a22");
    public static readonly MagickColor Text = new("#ffffff");

    public const int ACCENT_WIDTH = 24;
    public const int MARGIN = 96;
    private const int TITLE_MAX_LINES = 4;
    private static readonly double[] titleSizes = [76, 68, 60, 52];
    private const double LINE_HEIGHT = 1.15;
    public const int TITLE_TOP = 195;
    public const int TITLE_BOTTOM = SocialImage.HEIGHT - 165;

    private readonly IFileProvider webRoot = webRoot;

    public Result<byte[]> Generate(SocialImageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result.Failure<byte[]>("A title is required to generate a social image");
        }

        return ResolveAsset(FONT_TITLE)
            .Bind(title => ResolveAsset(FONT_LABEL).Map(label => (title, label)))
            .Bind(f => ResolveAsset(FONT_BYLINE).Map(byline => (f.title, f.label, byline)))
            .Bind(f => ResolveAsset(LOGO).Map(logo => (f.title, f.label, f.byline, logo)))
            .Bind(assets =>
            {
                try
                {
                    return Result.Success(Compose(request, assets.title, assets.label, assets.byline, assets.logo));
                }
                catch (MagickException ex)
                {
                    return Result.Failure<byte[]>($"Could not compose social image: {ex.Message}");
                }
            });
    }

    private byte[] Compose(SocialImageRequest request, string titleFont, string labelFont, string bylineFont, string logoPath)
    {
        using var image = new MagickImage(Background, SocialImage.WIDTH, SocialImage.HEIGHT);
        double textWidth = SocialImage.WIDTH - (2 * MARGIN);

        _ = new Drawables()
            .FillColor(Accent)
            .Rectangle(0, 0, ACCENT_WIDTH, SocialImage.HEIGHT)
            .Draw(image);

        // Logo + site label, top
        using var logo = RenderLogo(logoPath);
        int logoX = MARGIN - (int)Math.Round(logo.Height * LOGO_PADDING_X);
        int logoY = MARGIN - (int)Math.Round(logo.Height * LOGO_WORDMARK_TOP);
        image.Composite(logo, logoX, logoY, CompositeOperator.Over);

        double wordmarkBaseline = logoY + (logo.Height * LOGO_WORDMARK_BASELINE);
        _ = new Drawables()
            .Font(labelFont)
            .FontPointSize(LOGO_HEIGHT * LOGO_CAP_HEIGHT / 0.7)
            .FillColor(Text)
            .Text(logoX + logo.Width + (logo.Height * 0.13), wordmarkBaseline, SITE_LABEL)
            .Draw(image);

        // Title, vertically centred in the space between label and byline
        var block = SocialImageTextLayout.Fit(
            request.Title.Trim(),
            titleSizes,
            textWidth,
            TITLE_MAX_LINES,
            (text, size) => Measure(image, titleFont, size, text),
            TITLE_BOTTOM - TITLE_TOP,
            LINE_HEIGHT);

        double lineHeight = block.PointSize * LINE_HEIGHT;
        double blockHeight = lineHeight * block.Lines.Count;
        double top = TITLE_TOP;
        double bottom = TITLE_BOTTOM;
        double baseline = top + ((bottom - top - blockHeight) / 2) + block.PointSize;

        var title = new Drawables()
            .Font(titleFont)
            .FontPointSize(block.PointSize)
            .FillColor(Text);
        foreach (string line in block.Lines)
        {
            _ = title.Text(MARGIN, baseline, line);
            baseline += lineHeight;
        }
        _ = title.Draw(image);

        // Byline, bottom
        if (request.Byline.TryGetValue(out string? byline) && !string.IsNullOrWhiteSpace(byline))
        {
            var bylineBlock = SocialImageTextLayout.Fit(byline.Trim(), [36], textWidth, 1, (text, size) => Measure(image, bylineFont, size, text));
            _ = new Drawables()
                .Font(bylineFont)
                .FontPointSize(bylineBlock.PointSize)
                .FillColor(Text)
                .Text(MARGIN, SocialImage.HEIGHT - MARGIN + 10, bylineBlock.Lines[0])
                .Draw(image);
        }

        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    // logo.svg geometry (viewBox 444.45 x 150), as fractions of its height
    private const double LOGO_PADDING_X = 0;
    private const double LOGO_WORDMARK_TOP = 47.5 / 150;
    private const double LOGO_WORDMARK_BASELINE = 97.8 / 150;
    private const double LOGO_CAP_HEIGHT = 50.2 / 150;

    private static MagickImage RenderLogo(string logoPath)
    {
        // Wordmark paths have no fill (black); default them to white for the purple background
        string svg = File.ReadAllText(logoPath).Replace("<g>", "<g fill=\"#ffffff\">");
        var settings = new MagickReadSettings
        {
            Format = MagickFormat.Svg,
            BackgroundColor = MagickColors.None,
            Height = LOGO_HEIGHT,
            Width = (uint)Math.Round(LOGO_HEIGHT * 444.45 / 150),
        };

        return new MagickImage(System.Text.Encoding.UTF8.GetBytes(svg), settings);
    }

    private static double Measure(MagickImage image, string font, double pointSize, string text)
    {
        image.Settings.Font = font;
        image.Settings.FontPointsize = pointSize;

        return image.FontTypeMetrics(text)?.TextWidth ?? double.MaxValue;
    }

    private Result<string> ResolveAsset(string subpath)
    {
        var file = webRoot.GetFileInfo(subpath);

        return file.Exists && !string.IsNullOrEmpty(file.PhysicalPath)
            ? Result.Success(file.PhysicalPath)
            : Result.Failure<string>($"Brand asset [{subpath}] not found in the site static assets");
    }
}
