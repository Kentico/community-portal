using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.FAQ;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: FAQWidget.IDENTIFIER,
    viewComponentType: typeof(FAQWidget),
    name: FAQWidget.NAME,
    propertiesType: typeof(FAQWidgetProperties),
    Description = "Displays FAQ items in an expandable accordion format",
    IconClass = KenticoIcons.CHECKLIST,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.FAQ;

public class FAQWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Widgets.FAQ";
    public const string NAME = "FAQ";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<FAQWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        using var dependencyCollector = new CacheDependencyCollector();

        var (faqItems, faqGroup) = props.DataSourceParsed switch
        {
            FAQDataSources.FAQ_Group => await GetFAQItemsFromGroup(props),
            FAQDataSources.Individual_Items or _ => (await GetFAQItemsBySelection(props), Maybe<FAQGroupContent>.None)
        };

        cvm.CacheDependencies.CacheKeys = dependencyCollector.GetCacheDependency()?.CacheKeys ?? [];

        return Validate(faqItems, faqGroup, props)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/FAQ/FAQ.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<IReadOnlyList<FAQItemContent>> GetFAQItemsBySelection(FAQWidgetProperties props)
    {
        var faqItemGUIDs = (props.FAQItems ?? []).Select(i => i.Identifier).ToArray();

        if (faqItemGUIDs.Length == 0)
        {
            return [];
        }

        var faqItems = (await contentRetriever
            .RetrieveContentByGuids<FAQItemContent>(faqItemGUIDs))
            .OrderBy(item => Array.IndexOf(faqItemGUIDs, item.SystemFields.ContentItemGUID))
            .ToList();

        return faqItems;
    }

    private async Task<(IReadOnlyList<FAQItemContent> items, Maybe<FAQGroupContent> group)> GetFAQItemsFromGroup(FAQWidgetProperties props)
    {
        var groupGUID = (props.FAQGroup ?? []).Select(i => i.Identifier).TryFirst();

        if (!groupGUID.TryGetValue(out var guid))
        {
            return ([], Maybe<FAQGroupContent>.None);
        }

        var group = await contentRetriever
            .RetrieveContentByGuids<FAQGroupContent>(
                [guid],
                new RetrieveContentParameters { LinkedItemsMaxLevel = 1 })
            .TryFirst();

        if (!group.TryGetValue(out var faqGroup))
        {
            return ([], Maybe<FAQGroupContent>.None);
        }

        var faqItems = faqGroup.FAQGroupContentFAQItemContents.ToList();

        return (faqItems, faqGroup);
    }

    private Result<FAQWidgetViewModel, ComponentErrorViewModel> Validate(
        IReadOnlyList<FAQItemContent> items,
        Maybe<FAQGroupContent> faqGroup,
        FAQWidgetProperties props)
    {
        if (items.Count == 0)
        {
            string errorMsg = props.DataSourceParsed == FAQDataSources.FAQ_Group
                ? "Select an FAQ Group or ensure the selected group contains FAQ items."
                : "Select at least 1 FAQ Item.";

            return Result.Failure<FAQWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, errorMsg));
        }

        return new FAQWidgetViewModel(props, items, faqGroup);
    }
}

public class FAQWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Label",
        ExplanationText = "Optional heading displayed above the FAQ section",
        Order = 1
    )]
    public string Label { get; set; } = "";

    [DropDownComponent(
        Label = "Data Source",
        ExplanationText = "Choose between selecting individual FAQ items or an FAQ group",
        DataProviderType = typeof(EnumDropDownOptionsProvider<FAQDataSources>),
        Order = 2
    )]
    public string DataSource { get; set; } = nameof(FAQDataSources.Individual_Items);
    public FAQDataSources DataSourceParsed => EnumDropDownOptionsProvider<FAQDataSources>.Parse(DataSource, FAQDataSources.Individual_Items);

    [ContentItemSelectorComponent(
        contentTypeName: FAQItemContent.CONTENT_TYPE_NAME,
        Label = "FAQ Items",
        ExplanationText = "Select FAQ items to display",
        AllowContentItemCreation = true,
        Order = 3
    )]
    [VisibleIfEqualTo(
        nameof(DataSource),
        nameof(FAQDataSources.Individual_Items),
        StringComparison.OrdinalIgnoreCase
    )]
    public IEnumerable<ContentItemReference> FAQItems { get; set; } = [];

    [ContentItemSelectorComponent(
        contentTypeName: FAQGroupContent.CONTENT_TYPE_NAME,
        Label = "FAQ Group",
        ExplanationText = "Select an FAQ group containing FAQ items",
        AllowContentItemCreation = true,
        MaximumItems = 1,
        Order = 3
    )]
    [VisibleIfEqualTo(
        nameof(DataSource),
        nameof(FAQDataSources.FAQ_Group),
        StringComparison.OrdinalIgnoreCase
    )]
    public IEnumerable<ContentItemReference> FAQGroup { get; set; } = [];

    [CheckBoxComponent(
        Label = "Show Expand/Collapse All Controls",
        ExplanationText = "Display buttons to expand or collapse all FAQ items at once",
        Order = 4
    )]
    public bool ShowExpandCollapseControls { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show FAQ Group Title",
        ExplanationText = "Display the FAQ group's title. If enabled, this will override the widget label.",
        Order = 5
    )]
    [VisibleIfEqualTo(
        nameof(DataSource),
        nameof(FAQDataSources.FAQ_Group),
        StringComparison.OrdinalIgnoreCase
    )]
    public bool ShowGroupTitle { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show FAQ Group Description",
        ExplanationText = "Display the FAQ group's short description below the title",
        Order = 6
    )]
    [VisibleIfEqualTo(
        nameof(DataSource),
        nameof(FAQDataSources.FAQ_Group),
        StringComparison.OrdinalIgnoreCase
    )]
    public bool ShowGroupDescription { get; set; } = true;
}

public enum FAQDataSources
{
    [Description("Individual Items")]
    Individual_Items,
    [Description("FAQ Group")]
    FAQ_Group
}

public class FAQWidgetViewModel
{
    public string Label { get; }
    public string DisplayLabel { get; }
    public bool HasLabel { get; }
    public IReadOnlyList<FAQItemContent> FAQItems { get; }
    public bool ShowExpandCollapseControls { get; }
    public Guid WidgetInstanceID { get; }
    public string? GroupTitle { get; }
    public string? GroupDescription { get; }
    public bool ShowGroupTitle { get; }
    public bool ShowGroupDescription { get; }
    public bool HasGroupDescription { get; }
    public bool IsFromGroup { get; }

    public FAQWidgetViewModel(
        FAQWidgetProperties props,
        IReadOnlyList<FAQItemContent> items,
        Maybe<FAQGroupContent> faqGroup)
    {
        Label = props.Label;
        FAQItems = items;
        ShowExpandCollapseControls = props.ShowExpandCollapseControls;
        WidgetInstanceID = props.ID;
        IsFromGroup = props.DataSourceParsed == FAQDataSources.FAQ_Group;
        ShowGroupTitle = props.ShowGroupTitle;
        ShowGroupDescription = props.ShowGroupDescription;
        GroupTitle = faqGroup.TryGetValue(out var group) ? group.BasicItemTitle : null;
        GroupDescription = faqGroup.TryGetValue(out var grp) ? grp.BasicItemShortDescription : null;

        // Use group title if available and ShowGroupTitle is enabled, otherwise use widget label
        DisplayLabel = IsFromGroup && ShowGroupTitle && !string.IsNullOrWhiteSpace(GroupTitle)
            ? GroupTitle
            : Label;
        HasLabel = !string.IsNullOrWhiteSpace(DisplayLabel);
        HasGroupDescription = !string.IsNullOrWhiteSpace(GroupDescription);
    }
}
