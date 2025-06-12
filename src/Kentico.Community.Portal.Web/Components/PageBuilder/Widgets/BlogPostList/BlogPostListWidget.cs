using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CMS.ContentEngine;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.BlogPostList;
using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
    ITaxonomyRetriever taxonomyRetriever) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.BlogPost-List-Widget";
    public const string NAME = "Blog Post List";

    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IMediator mediator = mediator;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<BlogPostListWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var posts = props.BlogPostSourceParsed switch
        {
            BlogPostSources.Smart_Folder => await GetBlogPostsBySmartFolder(props),
            BlogPostSources.Individual_Selection => await GetBlogPostsBySelection(props),
            BlogPostSources.Post_Taxonomy => await GetBlogPostsByTaxonomy(props),
            BlogPostSources.Latest_Published or _ => await GetLatestBlogPosts(props),

        };

        return Validate(props, posts)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/BlogPostList/BlogPostList.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> GetBlogPostsByTaxonomy(BlogPostListWidgetProperties props)
    {
        var tagReferences = props.TagReferences;
        if (!tagReferences.Any())
        {
            return [];
        }

        var resp = await mediator.Send(new BlogPostsByTaxonomyQuery([.. tagReferences.Select(t => t.Identifier)], props.PostLimit, channelContext.WebsiteChannelName));
        var posts = resp.Items;
        return await BuildPostPageViewModels(posts, props);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> GetBlogPostsBySmartFolder(BlogPostListWidgetProperties props)
    {
        var folderReference = props.FolderReference;
        if (folderReference is null)
        {
            return [];
        }

        var resp = await mediator.Send(new BlogPostsBySmartFolderQuery(props.FolderReference.Identifier, props.PostLimit, channelContext.WebsiteChannelName));
        var posts = resp.Items;
        return await BuildPostPageViewModels(posts, props);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> GetBlogPostsBySelection(BlogPostListWidgetProperties props)
    {
        var selectedWebPageGUIDs = props.BlogPosts?.Select(a => a.WebPageGuid).ToArray() ?? [];
        if (selectedWebPageGUIDs.Length == 0)
        {
            return [];
        }

        var result = await mediator.Send(new BlogPostPagesByWebPageGUIDQuery(selectedWebPageGUIDs, channelContext.WebsiteChannelName));
        return await BuildPostPageViewModels(result.Items, props);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> GetLatestBlogPosts(BlogPostListWidgetProperties props)
    {
        var result = await mediator.Send(new BlogPostPagesLatestQuery(props.PostLimit, channelContext.WebsiteChannelName));
        return await BuildPostPageViewModels(result.Items, props);
    }

    private async Task<IReadOnlyList<BlogPostViewModel>> BuildPostPageViewModels(IEnumerable<BlogPostPage> pages, BlogPostListWidgetProperties props)
    {
        var vms = new List<BlogPostViewModel>();
        foreach (var page in pages)
        {
            var post = page.BlogPostPageBlogPostContent.FirstOrDefault();
            if (post is null)
            {
                continue;
            }

            var url = await urlRetriever.Retrieve(page);
            var author = await GetAuthor(page);
            var taxonomy = await GetTaxonomyName(props);

            var authorViewModel = new BlogPostAuthorViewModel(author);

            vms.Add(new BlogPostViewModel(page, authorViewModel, url, taxonomy));
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

    private static Result<BlogPostListWidgetViewModel, ComponentErrorViewModel> Validate(BlogPostListWidgetProperties props, IReadOnlyList<BlogPostViewModel> posts)
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

        return new BlogPostListWidgetViewModel(props, posts);
    }

    private async Task<Maybe<string>> GetTaxonomyName(BlogPostListWidgetProperties props)
    {
        if (props.BlogPostSourceParsed == BlogPostSources.Post_Taxonomy)
        {
            return Maybe<string>.None;
        }

        if (props.TagReferences.FirstOrDefault() is { } tagReference)
        {
            var tags = await taxonomyRetriever.RetrieveTags([tagReference.Identifier], CultureInfo.CurrentCulture.Name);

            return tags.TryFirst().Map(t => t.Title);
        }

        return Maybe<string>.None;
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

    [SmartFolderSelectorComponent(
        Label = "Smart folder",
        Order = 3,
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
        Label = "Blog Type",
        MaxSelectedTagsCount = 1,
        MinSelectedTagsCount = 1,
        ExplanationTextAsHtml = true,
        ExplanationText = """
        <p>The taxonomy tag assigned to the posts that are displayed.</p>
        """,
        Order = 3)]
    [VisibleIfEqualTo(
        nameof(BlogPostSource),
        nameof(BlogPostSources.Post_Taxonomy),
        StringComparison.OrdinalIgnoreCase
    )]
    public IEnumerable<TagReference> TagReferences { get; set; } = [];

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
    public IEnumerable<WebPageRelatedItem> BlogPosts { get; set; } = [];

    [NumberInputComponent(
        Label = "Post Limit",
        ExplanationText = "The maximum number of posts to display from the selected Taxonomies",
        Order = 4
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
        ExplanationText = "How the content is presented",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ItemLayout>),
        Order = 5
    )]
    public string ItemLayoutSource { get; set; } = nameof(ItemLayout.Minimal);
    public ItemLayout ItemLayoutSourceParsed => EnumDropDownOptionsProvider<ItemLayout>.Parse(ItemLayoutSource, ItemLayout.Minimal);
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
    [Description("Full")]
    Full,
    [Description("Minimal")]
    Minimal
}

public class BlogPostListWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = BlogPostListWidget.NAME;

    public Maybe<string> Heading { get; } = "";
    public IReadOnlyList<BlogPostViewModel> BlogPosts { get; set; } = [];
    public ItemLayout Layout { get; set; } = ItemLayout.Minimal;
    public string BlogType { get; set; } = "";

    public BlogPostListWidgetViewModel(BlogPostListWidgetProperties props, IReadOnlyList<BlogPostViewModel> posts)
    {
        Heading = Maybe.From(props.Heading).MapNullOrWhiteSpaceAsNone();
        BlogPosts = posts;
        Layout = props.ItemLayoutSourceParsed;
    }
}


public class BlogPostViewModel
{
    public string Title { get; } = "";
    public DateTime PublishedDate { get; }
    public BlogPostAuthorViewModel Author { get; }
    public string ShortDescription { get; }
    public string LinkPath { get; }
    public Maybe<string> Taxonomy { get; }

    public BlogPostViewModel(BlogPostPage page, BlogPostAuthorViewModel author, WebPageUrl url, Maybe<string> taxonomy)
    {
        Title = page.WebPageMetaTitle;
        ShortDescription = page.WebPageMetaShortDescription;
        PublishedDate = page.BlogPostPagePublishedDate;
        Author = author;
        LinkPath = url.RelativePath;
        Taxonomy = taxonomy;
    }
}

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
