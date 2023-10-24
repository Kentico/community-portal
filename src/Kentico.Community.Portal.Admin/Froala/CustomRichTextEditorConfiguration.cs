using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterRichTextEditorConfiguration(
    identifier: CustomRichTextEditorConfiguration.IDENTIFIER,
    configurationType: typeof(CustomRichTextEditorConfiguration),
    displayName: "Kentico Community")]

namespace Kentico.Community.Portal.Admin;

public class CustomRichTextEditorConfiguration : RichTextEditorConfiguration
{
    private const string ConfigurationPath = "Froala/RichTextEditorConfig.json";

    public const string IDENTIFIER = "Kentico.Community.Portal.RichText";

    public CustomRichTextEditorConfiguration() : base(ConfigurationPath)
    {
    }
}
