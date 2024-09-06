namespace Kentico.Community.Portal.Web.Components;

public class ComponentErrorViewModel
{
    public ComponentErrorViewModel(
        string componentName,
        ComponentType componentType,
        params string[] errors)
    {
        ComponentLabel = $"{componentName} {componentType}";
        Errors = errors;
    }

    public string ComponentLabel { get; }
    public IReadOnlyList<string> Errors { get; }
}

public enum ComponentType
{
    Widget,
    Section
}
