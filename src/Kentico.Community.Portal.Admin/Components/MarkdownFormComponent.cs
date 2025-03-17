using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(
   identifier: MarkdownFormComponent.IDENTIFIER,
   componentType: typeof(MarkdownFormComponent),
   name: "Markdown")]

namespace Kentico.Community.Portal.Admin;

[ComponentAttribute(typeof(MarkdownComponentAttribute))]
public class MarkdownFormComponent : FormComponent<MarkdownFormComponentClientProperties, string>
{
    public const string IDENTIFIER = "Kentico.Community.FormComponent.Markdown";

    // The name of client React component to invoke, without the 'FormComponent' suffix
    public override string ClientComponentName => "@kentico-community/portal-web-admin/Markdown";
}

// Client properties class
public class MarkdownFormComponentClientProperties : FormComponentClientProperties<string>
{
}

public class MarkdownComponentAttribute : FormComponentAttribute
{
}
