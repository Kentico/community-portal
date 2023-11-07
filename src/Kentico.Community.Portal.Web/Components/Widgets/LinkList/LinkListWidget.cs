using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Components.Widgets.LinkList;
using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Kentico.Community.Portal.Core.Operations;
using CMS.DataEngine;

[assembly: RegisterWidget(
    identifier: LinkListWidget.IDENTIFIER,
    viewComponentType: typeof(LinkListWidget),
    name: "Link List",
    propertiesType: typeof(LinkListWidgetProperties),
    Description = "A Link List Widget.",
    IconClass = "icon-dialog-window-cogwheel",
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.Widgets.LinkList;

public class LinkListWidget : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Widgets.LinkList";
    private readonly IMediator mediator;
    private readonly ICacheDependenciesScope scope;

    public LinkListWidget(IMediator mediator, ICacheDependenciesScope scope)
    {
        this.mediator = mediator;
        this.scope = scope;
    }

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<LinkListWidgetProperties> cvm)
    {
        var linkIDs = cvm.Properties.Links?.Select(l => l.Identifier).ToArray() ?? Array.Empty<Guid>();

        scope.Begin();

        var resp = await mediator.Send(new LinkContentsQuery(linkIDs));

        cvm.CacheDependencies.CacheKeys = scope.End().ToList();

        if (resp.Items.Count == 0)
        {
            ModelState.AddModelError("", "Select at least 1 Link Content item.");

            return View("~/Components/ComponentError.cshtml");
        }

        var vm = new LinkListWidgetViewModel(cvm.Properties, resp.Items);

        return cvm.Properties.DesignParsed switch
        {
            LinkListDesign.Call_To_Actions => View("~/Components/Widgets/LinkList/CallToActions.cshtml", vm),
            LinkListDesign.List_In_Card => View("~/Components/Widgets/LinkList/ListInCard.cshtml", vm),
            LinkListDesign.Link_List or _ => View("~/Components/Widgets/LinkList/LinkList.cshtml", vm)
        };
    }
}

public class LinkListWidgetProperties : IWidgetProperties
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
    public IEnumerable<ContentItemReference> Links { get; set; } = Enumerable.Empty<ContentItemReference>();

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
    [Description("Call to actions")]
    Call_To_Actions,
    [Description("List in Card")]
    List_In_Card,
}

public class LinkListWidgetViewModel
{
    public LinkListWidgetViewModel(LinkListWidgetProperties props, IReadOnlyList<LinkContent> links)
    {
        Label = props.Label;
        Links = links;
    }

    public string Label { get; }
    public IReadOnlyList<LinkContent> Links { get; }
}

public record LinkContentsQuery(Guid[] ContentItemGUIDs) : IQuery<LinkContentsQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => string.Join(",", ContentItemGUIDs);
}

public record LinkContentsQueryResponse(IReadOnlyList<LinkContent> Items);
public class LinkContentQueryHandler : ContentItemQueryHandler<LinkContentsQuery, LinkContentsQueryResponse>
{
    public LinkContentQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override async Task<LinkContentsQueryResponse> Handle(LinkContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(LinkContent.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .Where(w => w.WhereIn(nameof(ContentItemFields.ContentItemGUID), request.ContentItemGUIDs.ToArray()))
                .OrderBy(new[] { new OrderByColumn(nameof(LinkContent.LinkContentLabel), OrderDirection.Ascending) });
        });

        var contents = await Executor.GetResult(b, ContentItemMapper.Map<LinkContent>, DefaultQueryOptions, cancellationToken);

        return new(contents.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(LinkContentsQuery query, LinkContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(result.Items, (l, b) => b.ContentItem(l.SystemFields.ContentItemID));
}
