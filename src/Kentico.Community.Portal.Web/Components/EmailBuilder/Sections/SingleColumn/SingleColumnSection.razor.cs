using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.EmailBuilder.Sections.SingleColumn;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailSection(
    identifier: SingleColumnSection.IDENTIFIER,
    name: "Single column",
    componentType: typeof(SingleColumnSection),
    PropertiesType = typeof(SingleColumnSectionProperties),
    IconClass = KenticoIcons.SQUARE,
    Description = "Basic MJML section with a single column")]

namespace Kentico.Community.Portal.Web.Components.EmailBuilder.Sections.SingleColumn;

public partial class SingleColumnSection : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Templates.SingleColumn";

    [Parameter] public SingleColumnSectionProperties Properties { get; set; } = null!;
}

public class SingleColumnSectionProperties : IEmailSectionProperties
{
    [DropDownComponent(
        Label = "Design",
        ExplanationText = "The design of the section",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ContentDesigns>),
        Tooltip = "Select a design",
        Order = 1
    )]
    public string Design { get; set; } = nameof(ContentDesigns.Normal);
    public ContentDesigns DesignParsed => EnumDropDownOptionsProvider<ContentDesigns>.Parse(Design, ContentDesigns.Normal);

    [DropDownComponent(
        Label = "Margin",
        ExplanationText = "The margin around the section",
        Tooltip = "Select a margin",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ContentMargins>),
        Order = 2
    )]
    public string Margin { get; set; } = nameof(ContentMargins.Medium);
    public ContentMargins MarginParsed => EnumDropDownOptionsProvider<ContentMargins>.Parse(Margin, ContentMargins.Medium);
}

public enum ContentDesigns { Normal, Card }
public enum ContentMargins { None, Small, Medium, Large }
