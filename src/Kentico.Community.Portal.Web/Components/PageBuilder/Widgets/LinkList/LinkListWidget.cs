using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.LinkList;
using Kentico.Community.Portal.Web.Features.Community;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: LinkListWidget.IDENTIFIER,
    viewComponentType: typeof(LinkListWidget),
    name: LinkListWidget.NAME,
    propertiesType: typeof(LinkListWidgetProperties),
    Description = "A list of labeled URL links",
    IconClass = KenticoIcons.DIALOG_WINDOW_COGWHEEL)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.LinkList;

public class LinkListWidget(IMediator mediator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Widgets.LinkList";
    public const string NAME = "Link List";

    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<LinkListWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var linkIDs = (cvm.Properties.Links ?? []).Select(l => l.Identifier).ToArray();

        var resp = await mediator.Send(new LinkContentsQuery(linkIDs));

        return Validate(resp, props)
            .Match(
                vm => props.DesignParsed switch
                {
                    LinkListDesign.List_In_Card => View("~/Components/PageBuilder/Widgets/LinkList/ListInCard.cshtml", vm),
                    LinkListDesign.Link_List or _ => View("~/Components/PageBuilder/Widgets/LinkList/LinkList.cshtml", vm)
                },
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private Result<LinkListWidgetViewModel, ComponentErrorViewModel> Validate(LinkContentsQueryResponse resp, LinkListWidgetProperties props)
    {
        if (resp.Items.Count == 0)
        {
            return Result.Failure<LinkListWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "Select at least 1 Link Content item."));
        }

        return new LinkListWidgetViewModel(props, resp.Items);
    }
}

public class LinkListWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Label",
        Order = 1
    )]
    public string Label { get; set; } = "";

    [ContentItemSelectorComponent(
        contentTypeName: LinkContent.CONTENT_TYPE_NAME,
        Label = "Links",
        ExplanationText = "Link Content items to display in a list",
        AllowContentItemCreation = true,
        DefaultViewMode = ContentItemSelectorViewMode.List,
        Order = 2
    )]
    public IEnumerable<ContentItemReference> Links { get; set; } = [];

    [DropDownComponent(
        Label = "Design",
        ExplanationText = "Component layout and design",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkListDesign>),
        Order = 3
    )]
    public string DesignSource { get; set; } = nameof(LinkListDesign.Link_List);
    public LinkListDesign DesignParsed => EnumDropDownOptionsProvider<LinkListDesign>.Parse(DesignSource, LinkListDesign.Link_List);
}

public enum LinkListDesign
{
    [Description("Link list")]
    Link_List,
    [Description("List in Card")]
    List_In_Card,
}

public class LinkListWidgetViewModel(LinkListWidgetProperties props, IReadOnlyList<LinkContent> links)
{
    public string Label { get; } = props.Label;
    public IReadOnlyList<LinkContent> Links { get; } = links;
}
