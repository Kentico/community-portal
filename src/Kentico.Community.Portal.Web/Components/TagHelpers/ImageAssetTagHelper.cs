using CMS.ContentEngine;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

[HtmlTargetElement(ELEMENT_TAG, TagStructure = TagStructure.WithoutEndTag, Attributes = ATTRIBUTE_IMAGE)]
public class ImageAssetTagHelper(IWebsiteChannelContext channelContext) : TagHelper
{
    public const string ELEMENT_TAG = "img";
    public const string ATTRIBUTE_IMAGE = "xpc-image-asset";
    public const string ATTRIBUTE_LAZY = "xpc-image-asset-lazy";
    public const string ATTRIBUTE_RESPONSIVE = "xpc-image-asset-responsive";

    /// <summary>
    /// Standard image variant names with their maximum widths as configured in Xperience.
    /// These should match the variant names and max widths you've set up in your Xperience environment.
    /// Update these values to match your actual variant configuration.
    /// </summary>
    private static readonly Dictionary<string, int> standardVariants = new()
    {
        { "Small_Scaled", 400 },    // Mobile 1x displays
        { "Medium_Scaled", 800 },   // Mobile 2x, tablet 1x displays  
        { "Large_Scaled", 1200 },   // Mobile 3x, tablet 1.5x, desktop 1x displays
        { "Large_X2_Scaled", 1600 },      // Tablet 2x, desktop 1.3x displays
        { "Large_X3_Scaled", 2400 },     // Desktop 2x displays (optional)
    };
    private readonly IWebsiteChannelContext channelContext = channelContext;

    [HtmlAttributeName(ATTRIBUTE_IMAGE)]
    public Maybe<ImageAssetViewModel> Image { get; set; }

    [HtmlAttributeName(ATTRIBUTE_LAZY)]
    public bool Lazy { get; set; } = true;

    [HtmlAttributeName(ATTRIBUTE_RESPONSIVE)]
    public bool Responsive { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Image.TryGetValue(out var img))
        {
            output.SuppressOutput();

            return;
        }

        // Set basic image attributes
        output.Attributes.SetAttribute("src", img.Asset.Url);
        output.Attributes.SetAttribute("title", img.Title);
        output.Attributes.SetAttribute("alt", img.AltText);

        // Set dimensions if available
        if (img.Asset.Metadata.Width.HasValue)
        {
            output.Attributes.SetAttribute("width", img.Asset.Metadata.Width.Value);
        }
        if (img.Asset.Metadata.Height.HasValue)
        {
            output.Attributes.SetAttribute("height", img.Asset.Metadata.Height.Value);
        }

        if (Lazy)
        {
            output.Attributes.SetAttribute("loading", "lazy");
        }

        // Generate srcset for responsive images
        if (Responsive
            && img.Asset.Metadata.Width.HasValue
            && !channelContext.IsPreview)
        {
            string srcset = GenerateSrcSet(img.Asset, img.Asset.Metadata.Width.Value);
            if (!string.IsNullOrEmpty(srcset))
            {
                output.Attributes.SetAttribute("srcset", srcset);

                // Only set default sizes if not already specified
                if (!output.Attributes.ContainsName("sizes"))
                {
                    output.Attributes.SetAttribute("sizes", "100cqi");
                }
            }
        }
    }

    /// <summary>
    /// Generates srcset attribute value based on available image variants.
    /// Only includes variants that are smaller than the original image to avoid upscaling.
    /// </summary>
    /// <param name="asset">The ContentItemAsset containing variant information</param>
    /// <param name="originalWidth">The original image width to limit variant sizes</param>
    /// <returns>Srcset string or empty if no variants available</returns>
    private static string GenerateSrcSet(ContentItemAsset asset, int originalWidth)
    {
        if (asset.VariantUrls == null || asset.Metadata.Variants == null)
        {
            return string.Empty;
        }

        // Pre-allocate with reasonable capacity (typically 3-5 variants)
        var srcsetParts = new List<string>(6);

        // Process variants in order, building sorted list directly
        foreach (var (variantName, maxWidth) in standardVariants.OrderBy(kvp => kvp.Value))
        {
            // Skip variants that would be larger than the original image
            if (maxWidth >= originalWidth)
            {
                continue;
            }

            // Check if this variant exists and get its metadata
            if (asset.VariantUrls.TryGetValue(variantName, out string? variantUrl) &&
                asset.Metadata.Variants.TryGetValue(variantName, out var variantMetadata))
            {
                // Use actual width from Xperience metadata, fallback to max width
                int actualWidth = variantMetadata.Width ?? maxWidth;
                srcsetParts.Add($"{variantUrl} {actualWidth}w");
            }
        }

        // Always include the original image as the largest option
        srcsetParts.Add($"{asset.Url} {originalWidth}w");

        return string.Join(", ", srcsetParts);
    }
}
