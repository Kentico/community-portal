using System.Text.Encodings.Web;
using CMS.ContentEngine;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Web.Components.TagHelpers;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Tests.Components.TagHelpers;

/// <summary>
/// Pins the markup rendered by the image tag helpers for each original width boundary.
/// </summary>
public class ImageTagHelperMarkupTests
{
    /// <summary>
    /// Variants on the test asset. Actual widths are 1px under the max widths
    /// so the output shows which of the two is rendered.
    /// </summary>
    private static readonly (string Name, int Width)[] assetVariants =
    [
        ("Large_X3_Scaled", 2399),
        ("Small_Scaled", 399),
        ("Large_X2_Scaled", 1599),
        ("Medium_Scaled", 799),
        ("Large_Scaled", 1199),
        ("Unknown_Variant", 100),
    ];

    private static readonly object[] srcsetCases =
    [
        new object[] { 400, "/img/o.jpg 400w" },
        new object[] { 401, "/img/Small_Scaled.jpg 399w, /img/o.jpg 401w" },
        new object[] { 800, "/img/Small_Scaled.jpg 399w, /img/o.jpg 800w" },
        new object[] { 1200, "/img/Small_Scaled.jpg 399w, /img/Medium_Scaled.jpg 799w, /img/o.jpg 1200w" },
        new object[] { 1600, "/img/Small_Scaled.jpg 399w, /img/Medium_Scaled.jpg 799w, /img/Large_Scaled.jpg 1199w, /img/o.jpg 1600w" },
        new object[] { 2400, "/img/Small_Scaled.jpg 399w, /img/Medium_Scaled.jpg 799w, /img/Large_Scaled.jpg 1199w, /img/Large_X2_Scaled.jpg 1599w, /img/o.jpg 2400w" },
        new object[] { 3000, "/img/Small_Scaled.jpg 399w, /img/Medium_Scaled.jpg 799w, /img/Large_Scaled.jpg 1199w, /img/Large_X2_Scaled.jpg 1599w, /img/Large_X3_Scaled.jpg 2399w, /img/o.jpg 3000w" },
    ];

    [TestCaseSource(nameof(srcsetCases))]
    public void ImageTagHelper_Renders_Pinned_Markup(int originalWidth, string srcset)
    {
        var retriever = Substitute.For<IPageBuilderDataContextRetriever>();
        retriever.Retrieve().Returns(Substitute.For<IPageBuilderDataContext>());

        var sut = new ImageTagHelper(retriever)
        {
            Image = new ImageViewModel(CreateImage(originalWidth, withVariantWidths: true)),
        };

        string html = Render(sut);

        Assert.That(html, Is.EqualTo(
            $"""<img src="/img/o.jpg" title="Title" width="{originalWidth}" height="50" alt="Alt" loading="lazy" srcset="{srcset}" sizes="100cqi">"""));
    }

    [TestCaseSource(nameof(srcsetCases))]
    public void ImageAssetTagHelper_Renders_Pinned_Markup(int originalWidth, string srcset)
    {
        var sut = new ImageAssetTagHelper(Substitute.For<IWebsiteChannelContext>())
        {
            Image = new ImageAssetViewModel(CreateImage(originalWidth, withVariantWidths: true)),
        };

        string html = Render(sut);

        Assert.That(html, Is.EqualTo(
            $"""<img src="~/img/o.jpg" title="Title" alt="Alt" width="{originalWidth}" height="50" loading="lazy" srcset="{srcset}" sizes="100cqi">"""));
    }

    [Test]
    public void Variant_Without_Metadata_Width_Falls_Back_To_Declared_Max_Width()
    {
        var sut = new ImageAssetTagHelper(Substitute.For<IWebsiteChannelContext>())
        {
            Image = new ImageAssetViewModel(CreateImage(3000, withVariantWidths: false)),
        };

        string html = Render(sut);

        Assert.That(html, Is.EqualTo(
            """<img src="~/img/o.jpg" title="Title" alt="Alt" width="3000" height="50" loading="lazy" srcset="/img/Small_Scaled.jpg 400w, /img/Medium_Scaled.jpg 800w, /img/Large_Scaled.jpg 1200w, /img/Large_X2_Scaled.jpg 1600w, /img/Large_X3_Scaled.jpg 2400w, /img/o.jpg 3000w" sizes="100cqi">"""));
    }

    private static ImageContent CreateImage(int originalWidth, bool withVariantWidths)
    {
        var metadata = new ContentItemAssetMetadata { Width = originalWidth, Height = 50 };

        // Variants has no public setter (Xperience populates it on deserialization)
        typeof(ContentItemAssetMetadata)
            .GetProperty(nameof(ContentItemAssetMetadata.Variants))!
            .SetValue(metadata, assetVariants
                .ToDictionary(
                    v => v.Name,
                    v => new ContentItemAssetVariantMetadata { Width = withVariantWidths ? v.Width : null })
                .AsReadOnly());

        return new()
        {
            SystemFields = new ContentItemFields { ContentItemGUID = Guid.Empty },
            BasicItemTitle = "Title",
            BasicItemShortDescription = "Alt",
            ImageContentAsset = new ContentItemAsset
            {
                Url = "~/img/o.jpg",
                VariantUrls = assetVariants.ToDictionary(v => v.Name, v => $"~/img/{v.Name}.jpg").AsReadOnly(),
                Metadata = metadata,
            },
        };
    }

    private static string Render(TagHelper sut)
    {
        var context = new TagHelperContext([], new Dictionary<object, object>(), "id");
        var output = new TagHelperOutput(
            "img",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()))
        {
            TagMode = TagMode.StartTagOnly,
        };

        sut.Process(context, output);

        using var writer = new StringWriter();
        output.WriteTo(writer, HtmlEncoder.Default);

        return writer.ToString();
    }
}
