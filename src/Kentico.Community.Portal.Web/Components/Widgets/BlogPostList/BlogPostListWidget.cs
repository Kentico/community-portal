using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Components.Widgets.BlogPostList;
using Kentico.Community.Portal.Web.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Kentico.Community.Portal.Web.Features.Blog.Models;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Core.Operations;

[assembly: RegisterWidget(
    identifier: BlogPostListWidget.IDENTIFIER,
    viewComponentType: typeof(BlogPostListWidget),
    name: "Blog Post List",
    propertiesType: typeof(BlogPostListWidgetProperties),
    Description = "Displays a list of blog posts based on the widget properties.",
    IconClass = "icon-paragraph",
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.Widgets.BlogPostList;

public class BlogPostListWidget : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.BlogPost-List-Widget";

    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IMediator mediator;
    private readonly AssetItemService itemService;
    private readonly ICacheDependenciesScope scope;

    public BlogPostListWidget(
        IMediator mediator,
        IWebPageUrlRetriever urlRetriever,
        AssetItemService itemService,
        ICacheDependenciesScope scope)
    {
        this.urlRetriever = urlRetriever;
        this.mediator = mediator;
        this.itemService = itemService;
        this.scope = scope;
    }

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<BlogPostListWidgetProperties> cvm)
    {
        scope.Begin();

        var posts = cvm.Properties.BlogPostSourceParsed switch
        {
            BlogPostSources.Individual_Selection => await GetBlogPostsBySelection(cvm.Properties),
            BlogPostSources.Post_Taxonomy => await GetBlogPostsByTaxonomy(cvm.Properties),
            BlogPostSources.Latest_Published or _ => await GetLatestBlogPosts(cvm.Properties)
        };

        cvm.CacheDependencies.CacheKeys = scope.End().ToList();

        SetValidationErrors(cvm.Properties, posts);

        if (ModelState.ErrorCount > 0)
        {
            return View("~/Components/ComponentError.cshtml");
        }

        var vm = new BlogPostListWidgetViewModel(cvm.Properties, posts);
        return View("~/Components/Widgets/BlogPostList/BlogPostList.cshtml", vm);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> GetBlogPostsByTaxonomy(BlogPostListWidgetProperties props)
    {
        string taxnomyName = props.Taxonomy;
        if (string.IsNullOrWhiteSpace(taxnomyName))
        {
            return Array.Empty<BlogPostViewModel>();
        }

        var resp = await mediator.Send(new BlogPostsByTaxonomyQuery(taxnomyName, props.PostLimit));
        var posts = resp.Items;
        return await BuildPostPageViewModels(posts, props);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> GetBlogPostsBySelection(BlogPostListWidgetProperties props)
    {
        var selectedWebPageGUIDs = props.BlogPosts?.Select(a => a.WebPageGuid).ToList() ?? new();
        if (selectedWebPageGUIDs.Count == 0)
        {
            return Array.Empty<BlogPostViewModel>();
        }

        var result = await mediator.Send(new BlogPostPagesByWebPageGUIDQuery(selectedWebPageGUIDs.ToArray()));
        var posts = props.BlogPostSourceParsed == BlogPostSources.Individual_Selection
            ? result.Items.OrderBy(p => selectedWebPageGUIDs.IndexOf(p.SystemFields.WebPageItemGUID)).ToList()
            : result.Items;

        return await BuildPostPageViewModels(posts, props);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> GetLatestBlogPosts(BlogPostListWidgetProperties props)
    {
        var result = await mediator.Send(new BlogPostPagesLatestQuery(props.PostLimit));

        return await BuildPostPageViewModels(result.Items, props);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> BuildPostPageViewModels(IEnumerable<BlogPostPage> posts, BlogPostListWidgetProperties props)
    {
        var vms = new List<BlogPostViewModel>();

        foreach (var post in posts)
        {
            var url = await urlRetriever.Retrieve(post);
            var teaserImage = await itemService.RetrieveMediaFileImage(post.BlogPostPageTeaserMediaFileImage.FirstOrDefault());
            var author = await GetAuthor(post);
            var authorImage = await itemService.RetrieveMediaFileImage(author.AuthorContentPhotoMediaFileImage.FirstOrDefault());
            string? taxonomy = props.BlogPostSourceParsed == BlogPostSources.Post_Taxonomy
                ? null
                : post.BlogPostPageTaxonomy;

            vms.Add(new BlogPostViewModel()
            {
                Title = post.BlogPostPageTitle,
                Date = post.BlogPostPageDate,
                LinkPath = url.RelativePath,
                ShortDescription = post.BlogPostPageShortDescription,
                Author = new(author, authorImage, null), // TODO Connect to member
                TeaserImage = teaserImage,
                Taxonomy = taxonomy
            });
        }

        return vms;
    }

    private async Task<AuthorContent> GetAuthor(BlogPostPage post)
    {
        var author = post.BlogPostPageAuthor.FirstOrDefault();

        if (author is not null)
        {
            return author;
        }

        var resp = await mediator.Send(new AuthorContentQuery(AuthorContent.KENTICO_AUTHOR_CODE_NAME));

        if (resp.Author is null)
        {
            throw new Exception($"Missing Author [{AuthorContent.KENTICO_AUTHOR_CODE_NAME}] which is required to display Posts");
        }

        return author;
    }

    private void SetValidationErrors(BlogPostListWidgetProperties props, IReadOnlyList<BlogPostViewModel> posts)
    {
        if (posts.Count == 0)
        {
            string error = props.BlogPostSourceParsed switch
            {
                BlogPostSources.Individual_Selection => "Select a Post to display",
                BlogPostSources.Post_Taxonomy => "Select a Taxonomy with associated Posts",
                BlogPostSources.Latest_Published or _ => "There are no posts available to display"
            };
            ModelState.AddModelError("", error);
        }

        if (ModelState.ErrorCount > 0 && string.IsNullOrWhiteSpace(props.Heading))
        {
            ModelState.AddModelError("", "(optional) Add a Heading");
        }
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

    [DropDownComponent(
        Label = "Posts Source",
        ExplanationText = "The way that Posts are selected",
        DataProviderType = typeof(EnumDropDownOptionsProvider<BlogPostSources>),
        Order = 2
    )]
    public string BlogPostSource { get; set; } = nameof(BlogPostSources.Individual_Selection);
    public BlogPostSources BlogPostSourceParsed => EnumDropDownOptionsProvider<BlogPostSources>.Parse(BlogPostSource, BlogPostSources.Individual_Selection);

    [DropDownComponent(
        Label = "Taxonomies",
        ExplanationText = """
        <p>The taxonomy assigned to the posts that are displayed</p>
        """,
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(BlogPostTaxonomyDropDownOptionsProvider),
        Order = 3
    )]
    [VisibleIfEqualTo(
        nameof(BlogPostSource),
        nameof(BlogPostSources.Post_Taxonomy),
        StringComparison.OrdinalIgnoreCase
    )]
    public string Taxonomy { get; set; } = "";

    [NumberInputComponent(
        Label = "Post Limit",
        ExplanationText = "The maximum number of posts to display from the selected Taxonomies",
        Order = 4
    )]
    [VisibleIfEqualTo(
        nameof(BlogPostSource),
        nameof(BlogPostSources.Post_Taxonomy),
        StringComparison.OrdinalIgnoreCase
    )]
    [Range(1, 12)]
    public int PostLimit { get; set; } = 1;

    [WebPageSelectorComponent(
        Label = "Posts",
        Sortable = true,
        TreePath = "/Blog/Posts",
        MaximumPages = 4,
        ExplanationText = "The posts that are displayed",
        Order = 3)]
    [VisibleIfEqualTo(
        nameof(BlogPostSource),
        nameof(BlogPostSources.Individual_Selection),
        StringComparison.OrdinalIgnoreCase
    )]
    public IEnumerable<WebPageRelatedItem> BlogPosts { get; set; } = Enumerable.Empty<WebPageRelatedItem>();

    [DropDownComponent(
        Label = "Item Layout",
        ExplanationText = "How the content is presented",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ItemLayout>),
        Order = 5
    )]
    public string ItemLayoutSource { get; set; } = nameof(ItemLayout.Minimal);
    public ItemLayout ItemLayoutSourceParsed => EnumDropDownOptionsProvider<ItemLayout>.Parse(ItemLayoutSource, ItemLayout.Minimal);
}

public enum BlogPostSources
{
    [Description("Individual Selection")]
    Individual_Selection,
    [Description("Post Taxonomy")]
    Post_Taxonomy,
    [Description("Latest Published")]
    Latest_Published,
}

public enum ItemLayout
{
    [Description("Full")]
    Full,
    [Description("Minimal")]
    Minimal
}

