using System.ComponentModel;
using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.LinkList;
using Kentico.Community.Portal.Web.Features.Community;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
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

public class LinkListWidget(IMediator mediator, IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Widgets.LinkList";
    public const string NAME = "Link List";

    private readonly IMediator mediator = mediator;
    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<LinkListWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var links = props.DataSourceParsed switch
        {
            LinkListDataSource.Smart_Folder => await GetLinksBySmartFolder(props),
            LinkListDataSource.Individual_Selection or _ => await GetLinksBySelection(props)
        };

        var viewModels = await BuildLinkViewModels(links, props);

        return Validate(viewModels, props)
            .Match(
                vm => props.DesignParsed switch
                {
                    LinkListDesign.List_In_Card => View("~/Components/PageBuilder/Widgets/LinkList/ListInCard.cshtml", vm),
                    LinkListDesign.Link_List or _ => View("~/Components/PageBuilder/Widgets/LinkList/LinkList.cshtml", vm)
                },
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private async Task<IReadOnlyList<LinkContent>> GetLinksBySelection(LinkListWidgetProperties props)
    {
        var linkIDs = (props.Links ?? []).Select(l => l.Identifier).ToArray();
        var resp = await mediator.Send(new LinkContentsQuery(linkIDs));
        return resp.Items;
    }

    private async Task<IReadOnlyList<LinkContent>> GetLinksBySmartFolder(LinkListWidgetProperties props)
    {
        if (props.FolderReference is null)
        {
            return [];
        }

        var result = await contentRetriever.RetrieveContent<LinkContent>(
            RetrieveContentParameters.Default,
            q => q
                .InSmartFolder(props.FolderReference.Identifier)
                .OrderBy([new OrderByColumn(nameof(LinkContent.LinkContentPublishedDate), OrderDirection.Descending)]),
            new RetrievalCacheSettings($"SmartFolder|{props.FolderReference.Identifier}"));

        return [.. result];
    }

    private async Task<IReadOnlyList<LinkViewModel>> BuildLinkViewModels(IReadOnlyList<LinkContent> links, LinkListWidgetProperties props)
    {
        // Retrieve member names only for referenced member IDs
        Dictionary<int, string> memberNames = [];
        if (props.ShowAuthor)
        {
            int[] memberIDs = [.. links
                .Select(l => l.LinkContentMemberID)
                .Where(id => id > 0)
                .Distinct()];

            if (memberIDs.Length > 0)
            {
                var membersDict = await mediator.Send(new MembersAllQuery());
                memberNames = membersDict
                    .Where(kvp => memberIDs.Contains(kvp.Key))
                    .Select(kvp => kvp.Value.AsCommunityMember())
                    .ToDictionary(m => m.Id, m => m.DisplayName);
            }
        }

        // Batch retrieve all taxonomy tags at once
        Dictionary<Guid, string> taxonomyTitles = [];
        if (props.ShowDXTopics)
        {
            var allTagGuids = links
                .SelectMany(l => l.CoreTaxonomyDXTopics.Select(t => t.Identifier))
                .Distinct()
                .ToArray();

            if (allTagGuids.Length > 0)
            {
                var tagsResp = await mediator.Send(new TagsByTaxonomyCodeNameQuery(SystemTaxonomies.DXTopicTaxonomy.CodeName));
                taxonomyTitles = tagsResp.Tags
                    .Where(t => allTagGuids.Contains(t.Identifier))
                    .ToDictionary(t => t.Identifier, t => t.Title);
            }
        }

        // Build view models with preloaded data
        var vms = new List<LinkViewModel>();
        foreach (var link in links)
        {
            string authorName = link.LinkContentMemberID > 0 && memberNames.TryGetValue(link.LinkContentMemberID, out string? name)
                ? name
                : string.Empty;

            var dxTopics = props.ShowDXTopics
                ? link.CoreTaxonomyDXTopics
                    .Select(t => taxonomyTitles.TryGetValue(t.Identifier, out string? title) ? title : string.Empty)
                    .Where(t => !string.IsNullOrEmpty(t))
                : [];

            vms.Add(new LinkViewModel(link, dxTopics, authorName));
        }

        return vms;
    }

    private Result<LinkListWidgetViewModel, ComponentErrorViewModel> Validate(IReadOnlyList<LinkViewModel> links, LinkListWidgetProperties props)
    {
        if (links.Count == 0)
        {
            string error = props.DataSourceParsed switch
            {
                LinkListDataSource.Smart_Folder => "Select a smart folder with links",
                LinkListDataSource.Individual_Selection or _ => "Select at least 1 Link Content item"
            };
            return Result.Failure<LinkListWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, error));
        }

        return new LinkListWidgetViewModel
        {
            Label = props.Label,
            Links = links,
            ShowPublishedDate = props.ShowPublishedDate,
            ShowAuthor = props.ShowAuthor,
            ShowDXTopics = props.ShowDXTopics
        };
    }
}

public class LinkListWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Label",
        Order = 1
    )]
    public string Label { get; set; } = "";

    [DropDownComponent(
        Label = "Data Source",
        ExplanationText = "The way that Links are selected",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkListDataSource>),
        Order = 2
    )]
    public string DataSource { get; set; } = nameof(LinkListDataSource.Individual_Selection);
    public LinkListDataSource DataSourceParsed => EnumDropDownOptionsProvider<LinkListDataSource>.Parse(DataSource, LinkListDataSource.Individual_Selection);

    [SmartFolderSelectorComponent(
        Label = "Smart folder",
        Order = 3,
        ExplanationText = "Select a smart folder containing Link Content items",
        AllowedContentTypeIdentifiersFilter = typeof(LinkContentFilter))]
    [VisibleIfEqualTo(
        nameof(DataSource),
        nameof(LinkListDataSource.Smart_Folder),
        StringComparison.OrdinalIgnoreCase
    )]
    public SmartFolderReference FolderReference { get; set; } = null!;

    [ContentItemSelectorComponent(
        contentTypeName: LinkContent.CONTENT_TYPE_NAME,
        Label = "Links",
        ExplanationText = "Link Content items to display in a list",
        AllowContentItemCreation = true,
        DefaultViewMode = ContentItemSelectorViewMode.List,
        Order = 3
    )]
    [VisibleIfEqualTo(
        nameof(DataSource),
        nameof(LinkListDataSource.Individual_Selection),
        StringComparison.OrdinalIgnoreCase
    )]
    public IEnumerable<ContentItemReference> Links { get; set; } = [];

    [DropDownComponent(
        Label = "Design",
        ExplanationText = "Component layout and design",
        DataProviderType = typeof(EnumDropDownOptionsProvider<LinkListDesign>),
        Order = 4
    )]
    public string DesignSource { get; set; } = nameof(LinkListDesign.Link_List);
    public LinkListDesign DesignParsed => EnumDropDownOptionsProvider<LinkListDesign>.Parse(DesignSource, LinkListDesign.Link_List);

    [CheckBoxComponent(
        Label = "Show Published Date",
        ExplanationText = "Display the link's published date.",
        Order = 5
    )]
    public bool ShowPublishedDate { get; set; } = false;

    [CheckBoxComponent(
        Label = "Show Author",
        ExplanationText = "Display the link author's full name.",
        Order = 6
    )]
    public bool ShowAuthor { get; set; } = false;

    [CheckBoxComponent(
        Label = "Show DX Topics",
        ExplanationText = "Display the link's DX Topics taxonomy tags.",
        Order = 7
    )]
    public bool ShowDXTopics { get; set; } = false;
}

public enum LinkListDataSource
{
    [Description("Individual Selection")]
    Individual_Selection,
    [Description("Smart Folder")]
    Smart_Folder,
}

public enum LinkListDesign
{
    [Description("Link list")]
    Link_List,
    [Description("List in Card")]
    List_In_Card,
}

public class LinkListWidgetViewModel
{
    public string Label { get; set; } = "";
    public IReadOnlyList<LinkViewModel> Links { get; set; } = [];
    public bool ShowPublishedDate { get; set; }
    public bool ShowAuthor { get; set; }
    public bool ShowDXTopics { get; set; }
}

public class LinkViewModel
{
    public string Title { get; }
    public string ShortDescription { get; }
    public string PathOrURL { get; }
    public DateTime PublishedDate { get; }
    public int MemberID { get; }
    public string AuthorFullName { get; }
    public IEnumerable<string> DXTopics { get; }
    public Guid ContentItemGUID { get; }

    public LinkViewModel(LinkContent link, IEnumerable<string> dxTopics, string authorFullName)
    {
        Title = link.BasicItemTitle;
        ShortDescription = link.BasicItemShortDescription;
        PathOrURL = link.LinkContentPathOrURL;
        PublishedDate = link.LinkContentPublishedDate;
        MemberID = link.LinkContentMemberID;
        AuthorFullName = authorFullName;
        DXTopics = dxTopics;
        ContentItemGUID = link.SystemFields.ContentItemGUID;
    }
}

public class LinkContentFilter : IContentTypesNameFilter
{
    public IEnumerable<string> AllowedContentTypeNames { get; } = [LinkContent.CONTENT_TYPE_NAME];
}
