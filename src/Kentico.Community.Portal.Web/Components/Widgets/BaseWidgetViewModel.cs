namespace Kentico.Community.Portal.Web.Components.Widgets;

public abstract class BaseWidgetViewModel
{
    protected abstract string WidgetName { get; }

    public string ComponentName => $"{WidgetName} Widget";
}
