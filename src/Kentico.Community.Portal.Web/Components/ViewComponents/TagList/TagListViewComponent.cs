using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.TagList;

public class TagListViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IEnumerable<string> tags, TagListVariant variant = TagListVariant.Secondary, string? ariaLabel = null) =>
        View("~/Components/ViewComponents/TagList/TagList.cshtml", new TagListViewModel(tags, variant, ariaLabel));
}

public record TagListViewModel(IEnumerable<string> Tags, TagListVariant Variant, string? AriaLabel)
{
    public string ContainerAriaLabel => AriaLabel ?? $"{(Tags.Count() == 1 ? "Tag" : "Tags")}";
}

public enum TagListVariant
{
    Primary,
    Secondary,
    Secondary_300
}
