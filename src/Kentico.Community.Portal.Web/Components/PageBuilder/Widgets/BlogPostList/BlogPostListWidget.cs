using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CMS.ContentEngine;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.BlogPostList;
using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Kentico.Xperience.ComponentIcons;

[assembly: RegisterWidget(
    identifier: BlogPostListWidget.IDENTIFIER,
    viewComponentType: typeof(BlogPostListWidget),
    name: BlogPostListWidget.NAME,
    propertiesType: typeof(BlogPostListWidgetProperties),
    Description = "Displays a list of blog posts based on the widget properties.",
    IconClass = KenticoIcons.PARAGRAPH)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.BlogPostList;

public class BlogPostListWidget(
    IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext channelContext,
    ITaxonomyRetriever taxonomyRetriever,
    MarkdownRenderer markdownRenderer) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.BlogPost-List-Widget";
    public const string NAME = "Blog Post List";

    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IMediator mediator = mediator;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<BlogPostListWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        string instanceId = Guid.NewGuid().ToString("N")[..8];
        var posts = props.BlogPostSourceParsed switch
        {
            BlogPostSources.Smart_Folder => await GetBlogPostsBySmartFolder(props, instanceId),
            BlogPostSources.Individual_Selection => await GetBlogPostsBySelection(props, instanceId),
            BlogPostSources.Post_Taxonomy => await GetBlogPostsByTaxonomy(props, instanceId),
            BlogPostSources.Latest_Published or _ => await GetLatestBlogPosts(props, instanceId),

        };

        const string viewFolder = "~/Components/PageBuilder/Widgets/BlogPostList";
        string viewPath = props.ItemLayoutSourceParsed switch
        {
            ItemLayout.Cards => $"{viewFolder}/BlogPostList_Cards.cshtml",
            ItemLayout.Compact_List => $"{viewFolder}/BlogPostList_CompactList.cshtml",
            ItemLayout.Related_Callout => $"{viewFolder}/BlogPostList_RelatedCallout.cshtml",
            ItemLayout.Feature_Panel => $"{viewFolder}/BlogPostList_FeaturePanel.cshtml",
            ItemLayout.Spotlight => $"{viewFolder}/BlogPostList_Spotlight.cshtml",
            ItemLayout.Numbered_Index => $"{viewFolder}/BlogPostList_NumberedIndex.cshtml",
            ItemLayout.Featured_Mosaic => $"{viewFolder}/BlogPostList_FeaturedMosaic.cshtml",
            ItemLayout.Full => $"{viewFolder}/BlogPostList_Full.cshtml",
            ItemLayout.Minimal or _ => $"{viewFolder}/BlogPostList_Minimal.cshtml"
        };

        return Validate(props, posts, instanceId)
            .Match(
                vm => View(viewPath, vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private Maybe<HtmlString> RenderIntro(BlogPostListWidgetProperties props) =>
        Maybe.From(props.IntroContent)
            .MapNullOrWhiteSpaceAsNone()
            .Map(markdownRenderer.RenderUnsafe);

    private async Task<IReadOnlyList<BlogPostListItemViewModel>> GetBlogPostsByTaxonomy(BlogPostListWidgetProperties props, string instanceId)
    {
        var tagReferences = props.TagReferences;
        if (!tagReferences.Any())
        {
            return [];
        }

        var resp = await mediator.Send(new BlogPostsByTaxonomyQuery([.. tagReferences.Select(t => t.Identifier)], props.PostLimit, channelContext.WebsiteChannelName));
        var posts = resp.Items;
        return await BuildPostPageViewModels(posts, props, instanceId);
    }

    private async Task<IReadOnlyList<BlogPostListItemViewModel>> GetBlogPostsBySmartFolder(BlogPostListWidgetProperties props, string instanceId)
    {
        var folderReference = props.FolderReference;
        if (folderReference is null)
        {
            return [];
        }

        var resp = await mediator.Send(new BlogPostsBySmartFolderQuery(props.FolderReference.Identifier, props.PostLimit, channelContext.WebsiteChannelName));
        var posts = resp.Items;
        return await BuildPostPageViewModels(posts, props, instanceId);
    }

    private async Task<IReadOnlyList<BlogPostListItemViewModel>> GetBlogPostsBySelection(BlogPostListWidgetProperties props, string instanceId)
    {
        var selectedWebPageGUIDs = props.BlogPosts?.Select(a => a.WebPageGuid).ToArray() ?? [];
        if (selectedWebPageGUIDs.Length == 0)
        {
            return [];
        }

        var result = await mediator.Send(new BlogPostPagesByWebPageGUIDQuery(selectedWebPageGUIDs, channelContext.WebsiteChannelName));
        return await BuildPostPageViewModels(result.Items, props, instanceId);
    }

    private async Task<IReadOnlyList<BlogPostListItemViewModel>> GetLatestBlogPosts(BlogPostListWidgetProperties props, string instanceId)
    {
        var result = await mediator.Send(new BlogPostPagesLatestQuery(props.PostLimit, channelContext.WebsiteChannelName));
        return await BuildPostPageViewModels(result.Items, props, instanceId);
    }

    private async Task<IReadOnlyList<BlogPostListItemViewModel>> BuildPostPageViewModels(IEnumerable<BlogPostPage> pages, BlogPostListWidgetProperties props, string instanceId)
    {
        var vms = new List<BlogPostListItemViewModel>();
        foreach (var page in pages)
        {
            var post = page.BlogPostPageBlogPostContent.FirstOrDefault();
            if (post is null)
            {
                continue;
            }

            var url = await urlRetriever.Retrieve(page);
            var author = await GetAuthor(page);
            var blogTypeTag = await GetBlogPostTypeTag(page, props);
            var dxTopics = await GetDXTopicsTags(page, props);

            var authorViewModel = new BlogPostAuthorViewModel(author);

            vms.Add(new BlogPostListItemViewModel(page, url, authorViewModel, blogTypeTag, dxTopics, props, instanceId));
        }

        return vms;
    }

    private async Task<AuthorContent> GetAuthor(BlogPostPage page)
    {
        var author = page.BlogPostPageAuthorContent.FirstOrDefault();

        if (author is not null)
        {
            return author;
        }

        var resp = await mediator.Send(new AuthorContentQuery(AuthorContent.KENTICO_AUTHOR_CODE_NAME));

        if (resp.Author is null)
        {
            throw new Exception($"Missing Author [{AuthorContent.KENTICO_AUTHOR_CODE_NAME}] which is required to display Posts");
        }

        return resp.Author;
    }

    private Result<BlogPostListWidgetViewModel, ComponentErrorViewModel> Validate(BlogPostListWidgetProperties props, IReadOnlyList<BlogPostListItemViewModel> posts, string instanceId)
    {
        if (posts.Count == 0)
        {
            string error = props.BlogPostSourceParsed switch
            {
                BlogPostSources.Smart_Folder => "Select a smart folder",
                BlogPostSources.Individual_Selection => "Select a Post to display",
                BlogPostSources.Post_Taxonomy => "Select a Taxonomy with associated Posts",
                BlogPostSources.Latest_Published or _ => "There are no posts available to display"
            };
            return Result.Failure<BlogPostListWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, error));
        }

        return new BlogPostListWidgetViewModel(props, posts, RenderIntro(props), instanceId);
    }

    private async Task<Maybe<string>> GetBlogPostTypeTag(BlogPostPage page, BlogPostListWidgetProperties props)
    {
        // A single selected tag makes every post's type identical, so the badge adds nothing
        bool isSingleTaxonomy = props.BlogPostSourceParsed == BlogPostSources.Post_Taxonomy && props.TagReferences.Count() <= 1;
        if (isSingleTaxonomy || !props.ShowBlogType)
        {
            return Maybe<string>.None;
        }

        if (page.BlogPostPageBlogType.FirstOrDefault() is { } tagRef)
        {
            var tags = await taxonomyRetriever.RetrieveTags([tagRef.Identifier], CultureInfo.CurrentCulture.Name);

            return tags.TryFirst().Map(t => t.Title);
        }

        return Maybe<string>.None;
    }

    private async Task<IEnumerable<string>> GetDXTopicsTags(BlogPostPage page, BlogPostListWidgetProperties props)
    {
        if (!props.ShowDXTopics)
        {
            return [];
        }

        var tags = await taxonomyRetriever.RetrieveTags(page.CoreTaxonomyDXTopics.Select(t => t.Identifier), CultureInfo.CurrentCulture.Name);

        return tags.Select(t => t.Title);
    }
}

public class BlogPostListWidgetProperties : IWidgetProperties
{
    [TextInputComponent(
        Label = "Heading",
        ExplanationText = "List Heading",
        Tooltip = "Displays above the posts",
        Order = 1)]
    public string Heading { get; set; } = "";

    [MarkdownComponent(
        Label = "Intro content",
        ExplanationText = "Optional Markdown that introduces the list's purpose or context. Rendered below the heading.",
        Order = 2
    )]
    public string IntroContent { get; set; } = "";

    [DropDownComponent(
        Label = "Posts Source",
        ExplanationText = "The way that Posts are selected",
        DataProviderType = typeof(EnumDropDownOptionsProvider<BlogPostSources>),
        Order = 3
    )]
    public string BlogPostSource { get; set; } = nameof(BlogPostSources.Individual_Selection);
    public BlogPostSources BlogPostSourceParsed => EnumDropDownOptionsProvider<BlogPostSources>.Parse(BlogPostSource, BlogPostSources.Individual_Selection);

    [SmartFolderSelectorComponent(
        Label = "Smart folder",
        Order = 4,
        ExplanationText = "Select a smart folder containing Blog Post Content items",
        AllowedContentTypeIdentifiersFilter = typeof(BlogPostContentFilter))]
    [VisibleIfEqualTo(
        nameof(BlogPostSource),
        nameof(BlogPostSources.Smart_Folder),
        StringComparison.OrdinalIgnoreCase
    )]
    public SmartFolderReference FolderReference { get; set; } = null!;

    [TagSelectorComponent(
        SystemTaxonomies.BlogTypeTaxonomy.CodeName,
        Label = "Blog Types",
        MinSelectedTagsCount = 1,
        ExplanationTextAsHtml = true,
        ExplanationText = """
        <p>Posts assigned any of the selected taxonomy tags are displayed.</p>
        """,
        Order = 4)]
    [VisibleIfEqualTo(
        nameof(BlogPostSource),
        nameof(BlogPostSources.Post_Taxonomy),
        StringComparison.OrdinalIgnoreCase
    )]
    public IEnumerable<TagReference> TagReferences { get; set; } = [];

    [WebPageSelectorComponent(
        Label = "Posts",
        Sortable = true,
        TreePath = "/Blog/Posts-djbze2fn",
        MaximumPages = 4,
        ExplanationText = "The posts that are displayed",
        Order = 4)]
    [VisibleIfEqualTo(
        nameof(BlogPostSource),
        nameof(BlogPostSources.Individual_Selection),
        StringComparison.OrdinalIgnoreCase
    )]
    public IEnumerable<WebPageRelatedItem> BlogPosts { get; set; } = [];

    [NumberInputComponent(
        Label = "Post Limit",
        ExplanationText = "The maximum number of posts to display from the selected Taxonomies",
        Order = 5
    )]
    [VisibleIfNotEqualTo(
        nameof(BlogPostSource),
        nameof(BlogPostSources.Individual_Selection),
        StringComparison.OrdinalIgnoreCase
    )]
    [Range(1, 12)]
    public int PostLimit { get; set; } = 1;

    [DropDownComponent(
        Label = "Item Layout",
        ExplanationTextAsHtml = true,
        ExplanationText = """
        <p>How the content is presented.</p>
        <ul>
          <li><strong>Cards</strong> — grid of clickable cards, for several posts on a landing page.</li>
          <li><strong>Compact List</strong> — dense divided rows, for long lists and narrow columns.</li>
          <li><strong>Related Callout</strong> — tinted aside, for related reading inside a post.</li>
          <li><strong>Feature Panel</strong> — one elevated panel wrapping the heading and rows.</li>
          <li><strong>Spotlight</strong> — emphasizes the first post, remaining posts listed below.</li>
          <li><strong>Numbered Index</strong> — ordered list, for a series or a ranked set.</li>
          <li><strong>Featured Mosaic</strong> — image-led mosaic of up to 5 posts: a hero card, a tall card and three rows.</li>
        </ul>
        """,
        DataProviderType = typeof(EnumDropDownOptionsProvider<ItemLayout>),
        Order = 6
    )]
    public string ItemLayoutSource { get; set; } = nameof(ItemLayout.Minimal);
    public ItemLayout ItemLayoutSourceParsed => EnumDropDownOptionsProvider<ItemLayout>.Parse(ItemLayoutSource, ItemLayout.Minimal);

    [CheckBoxComponent(
        Label = "Show DX Topics",
        ExplanationText = "Display the selected blog posts' DX Topics taxonomy tags.",
        Order = 7
    )]
    [VisibleIfNotEqualTo(
        nameof(ItemLayoutSource),
        nameof(ItemLayout.Full),
        StringComparison.OrdinalIgnoreCase
    )]
    public bool ShowDXTopics { get; set; } = false;

    [CheckBoxComponent(
        Label = "Show Description",
        ExplanationText = "Display the blog post description.",
        Order = 8
    )]
    public bool ShowDescription { get; set; } = true;

    [NumberInputComponent(
        Label = "Description Line Limit",
        ExplanationText = "Truncate each description to this many lines with an ellipsis. Use 0 to show the full description.",
        Order = 9
    )]
    [VisibleIfEqualTo(nameof(ShowDescription), true)]
    [VisibleIfNotEqualTo(
        nameof(ItemLayoutSource),
        nameof(ItemLayout.Full),
        StringComparison.OrdinalIgnoreCase
    )]
    [VisibleIfNotEqualTo(
        nameof(ItemLayoutSource),
        nameof(ItemLayout.Minimal),
        StringComparison.OrdinalIgnoreCase
    )]
    [Range(0, 10)]
    public int DescriptionMaxLines { get; set; } = 0;

    [CheckBoxComponent(
        Label = "Show Author",
        ExplanationText = "Display the blog post author.",
        Order = 10
    )]
    public bool ShowAuthor { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show Published Date",
        ExplanationText = "Display the blog post published date.",
        Order = 11
    )]
    public bool ShowPublishedDate { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show Blog Type",
        ExplanationText = "Display the blog post type tag.",
        Order = 12
    )]
    [VisibleIfNotEqualTo(
        nameof(ItemLayoutSource),
        nameof(ItemLayout.Full),
        StringComparison.OrdinalIgnoreCase
    )]
    public bool ShowBlogType { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show Hero",
        ExplanationText = "Display the blog post hero image.",
        Order = 13
    )]
    [VisibleIfEqualTo(
        nameof(ItemLayoutSource),
        nameof(ItemLayout.Full),
        StringComparison.OrdinalIgnoreCase
    )]
    public bool ShowHero { get; set; } = true;
}

public enum BlogPostSources
{
    [Description("Smart Folder")]
    Smart_Folder,
    [Description("Individual Selection")]
    Individual_Selection,
    [Description("Post Taxonomy")]
    Post_Taxonomy,
    [Description("Latest Published")]
    Latest_Published,
}

public enum ItemLayout
{
    [Description("Cards")]
    Cards,
    [Description("Compact List")]
    Compact_List,
    [Description("Related Callout")]
    Related_Callout,
    [Description("Feature Panel")]
    Feature_Panel,
    [Description("Spotlight")]
    Spotlight,
    [Description("Numbered Index")]
    Numbered_Index,
    [Description("Featured Mosaic")]
    Featured_Mosaic,
    [Description("Full (legacy)")]
    Full,
    [Description("Minimal (legacy)")]
    Minimal
}

public class BlogPostListWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = BlogPostListWidget.NAME;

    public Maybe<string> Heading { get; }
    public Maybe<HtmlString> Intro { get; }
    public IReadOnlyList<BlogPostListItemViewModel> BlogPosts { get; } = [];
    public ItemLayout Layout { get; } = ItemLayout.Minimal;
    public bool ShowHero { get; }

    /// <summary>
    /// The heading doubles as the eyebrow/kicker label in the Related Callout layout.
    /// </summary>
    public bool HasHeader => Heading.HasValue || Intro.HasValue;

    public BlogPostListWidgetViewModel(BlogPostListWidgetProperties props, IReadOnlyList<BlogPostListItemViewModel> posts, Maybe<HtmlString> intro, string instanceId)
    {
        Heading = Maybe.From(props.Heading).MapNullOrWhiteSpaceAsNone();
        Intro = intro;
        BlogPosts = posts;
        Layout = props.ItemLayoutSourceParsed;
        ShowHero = props.ShowHero;
        InstanceID = instanceId;
    }

    /// <summary>
    /// Unique per rendered widget instance. Post identifiers alone are not enough to key
    /// element IDs: two lists on the same page can include the same post, which would emit
    /// duplicate IDs and make their ARIA references ambiguous.
    /// </summary>
    public string InstanceID { get; }
}

/// <summary>
/// A single post already resolved against its list's display settings, ready for the
/// shared item partials (<c>_Meta</c>, <c>_Description</c>, <c>_Tags</c>) to render.
/// </summary>
public class BlogPostListItemViewModel
{
    public string Title { get; } = "";
    public string LinkPath { get; } = "";
    public Maybe<DateTime> PublishedDate { get; }
    public Maybe<BlogPostAuthorViewModel> Author { get; }
    public Maybe<string> Description { get; }
    public int DescriptionMaxLines { get; }
    public Maybe<string> BlogType { get; }
    public IReadOnlyList<string> DXTopics { get; } = [];

    /// <summary>
    /// The post's featured image, for the image-led layouts. Most posts have none.
    /// </summary>
    public Maybe<ImageViewModel> Image { get; }

    /// <summary>
    /// Stable 0-255 value derived from the post's identifier. Image-led layouts use it
    /// to pick a decorative fallback when <see cref="Image"/> is missing, so the same
    /// post always gets the same fallback.
    /// </summary>
    public int FallbackSeed { get; }

    /// <summary>
    /// Stable per-post identifier, used to generate unique element IDs for ARIA relationships.
    /// </summary>
    public Guid ID { get; }

    /// <summary>
    /// ID of the rendered description, for the title link's <c>aria-describedby</c>.
    /// </summary>
    public string DescriptionID { get; } = "";

    /// <summary>
    /// Blog type and DX Topics merged into a single badge row, for the layouts that
    /// render them together rather than as separate <c>_Tags</c> groups.
    /// </summary>
    public IReadOnlyList<string> Tags
    {
        get
        {
            List<string> tags = [];
            if (BlogType.TryGetValue(out string? blogType))
            {
                tags.Add(blogType);
            }

            tags.AddRange(DXTopics);
            return tags;
        }
    }

    public BlogPostListItemViewModel(
        BlogPostPage page,
        WebPageUrl url,
        BlogPostAuthorViewModel author,
        Maybe<string> blogTypeTag,
        IEnumerable<string> dxTopics,
        BlogPostListWidgetProperties props,
        string instanceId)
    {
        ID = page.SystemFields.WebPageItemGUID;
        Title = page.BasicItemTitle;
        LinkPath = url.RelativePath;
        PublishedDate = props.ShowPublishedDate ? page.BlogPostPagePublishedDate : Maybe<DateTime>.None;
        Author = props.ShowAuthor && !string.IsNullOrWhiteSpace(author.Name) ? author : Maybe<BlogPostAuthorViewModel>.None;
        Description = props.ShowDescription
            ? Maybe.From(page.BasicItemShortDescription).MapNullOrWhiteSpaceAsNone()
            : Maybe<string>.None;
        DescriptionMaxLines = props.DescriptionMaxLines;
        BlogType = blogTypeTag;
        DXTopics = [.. dxTopics];
        Image = page.FeaturedImageImageContent is null
            ? Maybe<ImageViewModel>.None
            : page.ToImageViewModel();
        FallbackSeed = ID.ToByteArray()[0];
        DescriptionID = $"blog-post-desc-{instanceId}-{ID:N}";
    }
}

/// <summary>
/// One card of the Featured Mosaic layout: the post plus the card variant's modifier
/// class and the <c>sizes</c> hint matching that variant's image slot.
/// </summary>
public record BlogPostMosaicCardViewModel(BlogPostListItemViewModel Post, string VariantClass, string ImageSizes);

public class AuthorViewModel
{
    public string Name { get; init; } = "";
    public Maybe<string> Title { get; init; }
    public Maybe<ImageViewModel> Photo { get; init; }
    public Maybe<string> LinkProfilePath { get; init; }

    public AuthorViewModel(AuthorContent author, IUrlHelper urlHelper)
    {
        Name = author.FullName;
        Photo = author.AuthorContentPhotoImageContent
            .TryFirst()
            .Map(ImageViewModel.Create);
        LinkProfilePath = author.AuthorContentMemberID == 0
            ? Maybe<string>.None
            : urlHelper.Action(nameof(MemberController.MemberDetail), "Member", new { memberID = author.AuthorContentMemberID });
    }
}

public class BlogPostContentFilter : IContentTypesNameFilter
{
    public IEnumerable<string> AllowedContentTypeNames { get; } = [BlogPostContent.CONTENT_TYPE_NAME];

}
