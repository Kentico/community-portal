namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Heading;

/// <summary>
/// Manages heading anchor slugs for a single request to ensure uniqueness across the page
/// </summary>
public interface IHeadingContext
{
    /// <summary>
    /// Generates a unique slug for a heading, appending a suffix if the slug already exists
    /// </summary>
    /// <param name="headingText">The heading text to generate a slug from</param>
    /// <returns>A unique slug for the heading</returns>
    public string GetUniqueSlug(string headingText);
}
