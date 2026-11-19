using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.ComponentIcons;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Components.FormBuilder.Sections.TitledGroup;

[assembly: RegisterFormSection(
    identifier: TitledGroupSection.IDENTIFIER,
    viewComponentType: typeof(TitledGroupSection),
    name: "Titled group",
    PropertiesType = typeof(TitledGroupSectionProperties),
    Description = "Organizes form fields into a visually grouped section with a configurable title.",
    IconClass = KenticoIcons.RECTANGLE_PARAGRAPH)]

namespace Kentico.Community.Portal.Web.Components.FormBuilder.Sections.TitledGroup;

public class TitledGroupSection : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.FormSection.TitledGroup";

    public IViewComponentResult Invoke(FormSectionViewModel<TitledGroupSectionProperties> viewModel) =>
        View("~/Components/FormBuilder/Sections/TitledGroup/TitledGroupSection.cshtml", viewModel);
}

public class TitledGroupSectionProperties : IFormSectionProperties
{
    [TextInputComponent(
        Label = "Section title",
        ExplanationText = "Optional heading shown above grouped form fields.",
        Order = 0)]
    public string Title { get; set; } = "Section";
}
