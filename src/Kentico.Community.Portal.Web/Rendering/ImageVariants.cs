namespace Kentico.Community.Portal.Web.Rendering;

/// <summary>
/// An image variant code name and its maximum width in pixels.
/// </summary>
public readonly record struct ImageVariant(string Name, int MaxWidth);

/// <summary>
/// Single source of truth for the image variants configured in Xperience
/// (Standard media dimensions, serialized in the CI repository under <c>cms.standardmediadimensions</c>).
/// Names and max widths must match that configuration.
/// </summary>
public static class ImageVariants
{
    /// <summary>Mobile 1x displays</summary>
    public static readonly ImageVariant Small = new("Small_Scaled", 400);
    /// <summary>Mobile 2x, tablet 1x displays</summary>
    public static readonly ImageVariant Medium = new("Medium_Scaled", 800);
    /// <summary>Mobile 3x, tablet 1.5x, desktop 1x displays</summary>
    public static readonly ImageVariant Large = new("Large_Scaled", 1200);
    /// <summary>Tablet 2x, desktop 1.3x displays</summary>
    public static readonly ImageVariant LargeX2 = new("Large_X2_Scaled", 1600);
    /// <summary>Desktop 2x displays</summary>
    public static readonly ImageVariant LargeX3 = new("Large_X3_Scaled", 2400);

    /// <summary>
    /// All variants, ordered by ascending <see cref="ImageVariant.MaxWidth"/>.
    /// </summary>
    public static IReadOnlyList<ImageVariant> All { get; } = [Small, Medium, Large, LargeX2, LargeX3];
}
