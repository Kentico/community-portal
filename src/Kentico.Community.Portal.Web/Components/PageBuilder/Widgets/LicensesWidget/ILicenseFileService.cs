namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Licenses;

/// <summary>
/// Service for reading and processing license files from content hub assets.
/// </summary>
public interface ILicenseFileService
{
    /// <summary>
    /// Retrieves licenses from a file content asset and formats them with type links.
    /// </summary>
    /// <param name="file">The file content containing license data</param>
    /// <param name="licenseTypeLinks">Dictionary mapping license type names to their URLs</param>
    /// <returns>A view model containing formatted license links</returns>
    public LicensesViewModel GetLicenses(FileContent file, Dictionary<string, string> licenseTypeLinks);
}
