namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Settings for <see cref="ConsentGatedCodeSnippetsService"/>.
/// </summary>
public class TagManagerConsentOptions
{
    public const string SECTION = "TagManagerConsentOptions";

    /// <summary>
    /// Code names (<c>ChannelCodeSnippetItemName</c>) of Tag Manager snippets that render without
    /// Analytics consent. Empty by default: no snippet renders until the visitor allows Analytics.
    /// </summary>
    public List<string> AlwaysAllowedSnippetNames { get; set; } = [];
}
