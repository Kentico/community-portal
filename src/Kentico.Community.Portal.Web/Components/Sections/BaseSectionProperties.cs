using Kentico.PageBuilder.Web.Mvc;

namespace Kentico.Community.Portal.Web.Components.Sections;

public abstract class BaseSectionProperties : ISectionProperties
{
    public Guid ID { get; set; } = Guid.NewGuid();
}

