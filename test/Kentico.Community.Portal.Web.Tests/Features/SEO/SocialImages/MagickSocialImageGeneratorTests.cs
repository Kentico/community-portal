using CSharpFunctionalExtensions;
using ImageMagick;
using Kentico.Community.Portal.Web.Features.SEO.SocialImages;
using Microsoft.Extensions.FileProviders;

namespace Kentico.Community.Portal.Web.Tests.Features.SEO.SocialImages;

public class MagickSocialImageGeneratorTests
{
    private const string LONG_TITLE =
        "Xperience by Kentico Refresh walkthrough: migrating content types, reusable field schemas, " +
        "Page Builder widgets, and custom admin UI pages without losing a single editor's mind along the way";

    private static MagickSocialImageGenerator CreateGenerator() =>
        new(new PhysicalFileProvider(Path.Combine(TestContext.CurrentContext.TestDirectory, "wwwroot")));

    [Test]
    public void Generates_Png_At_Open_Graph_Size()
    {
        var result = CreateGenerator().Generate(new("Hello Xperience", Maybe.From("Sean Wright")));

        Assert.That(result.IsSuccess, Is.True, () => result.Error);
        using var image = new MagickImage(result.Value);
        Assert.Multiple(() =>
        {
            Assert.That(image.Format, Is.EqualTo(MagickFormat.Png));
            Assert.That(image.Width, Is.EqualTo((uint)SocialImage.WIDTH));
            Assert.That(image.Height, Is.EqualTo((uint)SocialImage.HEIGHT));
        });
    }

    [Test]
    public void Uses_Brand_Background_And_Accent()
    {
        var result = CreateGenerator().Generate(new("Hello Xperience", Maybe<string>.None));

        using var image = new MagickImage(result.Value);
        using var pixels = image.GetPixels();
        Assert.Multiple(() =>
        {
            Assert.That(pixels.GetPixel(SocialImage.WIDTH - 1, SocialImage.HEIGHT - 1).ToColor(), Is.EqualTo(MagickSocialImageGenerator.Background));
            Assert.That(pixels.GetPixel(MagickSocialImageGenerator.ACCENT_WIDTH / 2, SocialImage.HEIGHT / 2).ToColor(), Is.EqualTo(MagickSocialImageGenerator.Accent));
        });
    }

    [Test]
    public void Renders_Title_Text()
    {
        var result = CreateGenerator().Generate(new("Hello Xperience", Maybe<string>.None));

        using var image = new MagickImage(result.Value);
        using var pixels = image.GetPixels();
        int textPixels = 0;
        for (int y = MagickSocialImageGenerator.TITLE_TOP; y < MagickSocialImageGenerator.TITLE_BOTTOM; y++)
        {
            for (int x = MagickSocialImageGenerator.MARGIN; x < SocialImage.WIDTH - MagickSocialImageGenerator.MARGIN; x++)
            {
                if (Equals(pixels.GetPixel(x, y).ToColor(), MagickSocialImageGenerator.Text))
                {
                    textPixels++;
                }
            }
        }

        Assert.That(textPixels, Is.GreaterThan(0));
    }

    [Test]
    public void Long_Title_Stays_Inside_Right_Margin()
    {
        var result = CreateGenerator().Generate(new(LONG_TITLE, Maybe.From("A Very Long Author Name With Several Middle Names")));

        Assert.That(result.IsSuccess, Is.True, () => result.Error);
        using var image = new MagickImage(result.Value);
        using var pixels = image.GetPixels();
        int x = SocialImage.WIDTH - (MagickSocialImageGenerator.MARGIN / 2);
        for (int y = 0; y < SocialImage.HEIGHT; y++)
        {
            Assert.That(pixels.GetPixel(x, y).ToColor(), Is.EqualTo(MagickSocialImageGenerator.Background), $"Text overflowed at ({x}, {y})");
        }
    }

    [Test]
    public void Renders_Logo_Top_Left()
    {
        var result = CreateGenerator().Generate(new("Hello Xperience", Maybe<string>.None));

        using var image = new MagickImage(result.Value);
        using var pixels = image.GetPixels();
        int logoPixels = 0;
        int right = MagickSocialImageGenerator.MARGIN + MagickSocialImageGenerator.LOGO_HEIGHT;
        for (int y = 0; y < MagickSocialImageGenerator.TITLE_TOP; y++)
        {
            for (int x = MagickSocialImageGenerator.ACCENT_WIDTH; x < right; x++)
            {
                if (Equals(pixels.GetPixel(x, y).ToColor(), MagickSocialImageGenerator.Accent))
                {
                    logoPixels++;
                }
            }
        }

        Assert.That(logoPixels, Is.GreaterThan(1000), "Expected the orange logo mark in the top-left");
    }

    [Test]
    public void Missing_Logo_Fails()
    {
        string root = Directory.CreateTempSubdirectory().FullName;
        string fonts = Directory.CreateDirectory(Path.Combine(root, "fonts")).FullName;
        foreach (string font in Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "wwwroot", "fonts")))
        {
            File.Copy(font, Path.Combine(fonts, Path.GetFileName(font)));
        }
        var generator = new MagickSocialImageGenerator(new PhysicalFileProvider(root));

        var result = generator.Generate(new("Hello Xperience", Maybe<string>.None));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Does.Contain(MagickSocialImageGenerator.LOGO));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Empty_Title_Fails(string title) =>
        Assert.That(CreateGenerator().Generate(new(title, Maybe<string>.None)).IsFailure, Is.True);

    [Test]
    public void Missing_Brand_Fonts_Fail()
    {
        string emptyRoot = Directory.CreateTempSubdirectory().FullName;
        var generator = new MagickSocialImageGenerator(new PhysicalFileProvider(emptyRoot));

        var result = generator.Generate(new("Hello Xperience", Maybe<string>.None));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Does.Contain(MagickSocialImageGenerator.FONT_TITLE));
        });
    }
}
