using System.ComponentModel;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Sections;

/// <summary>
/// The background colors available to every section. Shared so the Single Column and Grid
/// sections offer editors an identical set of options.
/// </summary>
public enum SectionBackgroundColors
{
    [Description("Light")]
    Light,
    [Description("White")]
    White,
    [Description("Primary Light")]
    Primary_Light,
    [Description("Secondary Light")]
    Secondary_Light
}
