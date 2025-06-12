namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets;

public abstract class BaseWidgetViewModel
{
    protected abstract string WidgetName { get; }

    public string ComponentName => $"{WidgetName} Widget";
}
