using Kentico.PageBuilder.Web.Mvc;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets;

public abstract class BaseWidgetProperties : IWidgetProperties
{
    public Guid ID { get; set; } = Guid.NewGuid();
}

