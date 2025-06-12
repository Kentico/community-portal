namespace Kentico.Community.Portal.Web.Components.FormBuilder;

/// <summary>
/// A component that translates to &lt;input type="hidden"&gt;
/// when rendered on a live website.
/// </summary>
public interface IHiddenInputComponent;
/// <summary>
/// A component that _can_ be hidden from rendering on a live website.
/// </summary>
public interface IHideableComponent
{
    public bool IsHidden { get; }
}
