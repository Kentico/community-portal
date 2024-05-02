using System.ComponentModel;
using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components.Widgets.LinkList;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: LinkListWidget.IDENTIFIER,
    viewComponentType: typeof(LinkListWidget),
    name: "Link List",
    propertiesType: typeof(LinkListWidgetProperties),
    Description = "A Link List Widget.",
    IconClass = "icon-dialog-window-cogwheel",
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.Widgets.LinkList;

public class LinkListWidget(IMediator mediator, ICacheDependenciesScope scope) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Widgets.LinkList";
    private readonly IMediator mediator = mediator;
    private readonly ICacheDependenciesScope scope = scope;

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

public class LinkListWidgetViewModel(LinkListWidgetProperties props, IReadOnlyList<LinkContent> links)
{
    public string Label { get; } = props.Label;
    public IReadOnlyList<LinkContent> Links { get; } = links;
}

public record LinkContentsQuery(Guid[] ContentItemGUIDs) : IQuery<LinkContentsQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => string.Join(",", ContentItemGUIDs);
}

public record LinkContentsQueryResponse(IReadOnlyList<LinkContent> Items);
public class LinkContentQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<LinkContentsQuery, LinkContentsQueryResponse>(tools)
{
    public override async Task<LinkContentsQueryResponse> Handle(LinkContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(LinkContent.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .Where(w => w.WhereIn(nameof(ContentItemFields.ContentItemGUID), request.ContentItemGUIDs.ToArray()))
                .OrderBy(new[] { new OrderByColumn(nameof(LinkContent.LinkContentLabel), OrderDirection.Ascending) });
        });

        var contents = await Executor.GetMappedResult<LinkContent>(b, DefaultQueryOptions, cancellationToken);

        return new(contents.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(LinkContentsQuery query, LinkContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(result.Items, (l, b) => b.ContentItem(l.SystemFields.ContentItemID));
}
