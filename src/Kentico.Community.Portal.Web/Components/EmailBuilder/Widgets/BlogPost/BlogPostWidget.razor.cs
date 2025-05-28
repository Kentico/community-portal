using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.BlogPost;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: BlogPostWidget.IDENTIFIER,
    name: "Blog post",
    componentType: typeof(BlogPostWidget),
    IconClass = KenticoIcons.PARAGRAPH_CENTER,
    PropertiesType = typeof(BlogPostWidgetProperties),
    Description = "Displays a blog post in a card.")]

namespace Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.BlogPost;

public partial class BlogPostWidget : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Widgets.BlogPost";

    private EmailContext? emailContext;

    [Inject] public required IContentQueryExecutor QueryExecutor { get; set; }
    [Inject] public required ITaxonomyRetriever TaxonomyRetriever { get; set; }
    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }
    [Inject] public required IContentRetriever ContentRetriever { get; set; }

    [Parameter] public required BlogPostWidgetProperties Properties { get; set; }

    public Maybe<BlogPostPage> Model { get; set; } = Maybe<BlogPostPage>.None;
    public Dictionary<Guid, Tag> BlogTypes { get; set; } = [];
    public Dictionary<Guid, Tag> DXTopics { get; set; } = [];
    public EmailContext EmailContext => emailContext ??= EmailContextAccessor.GetContext();

    protected override async Task OnInitializedAsync()
    {
        if (!Properties.SelectedBlogPosts.Any())
        {
            return;
        }

        var emailContext = EmailContextAccessor.GetContext();

        BlogTypes = (await TaxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.BlogType.TaxonomyName, PortalWebSiteChannel.DEFAULT_LANGUAGE))
            .Tags
            .ToDictionary(t => t.Identifier);
        DXTopics = (await TaxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.DXTopic.TaxonomyName, PortalWebSiteChannel.DEFAULT_LANGUAGE))
            .Tags
            .ToDictionary(t => t.Identifier);

        // TODO replace with Content Retriever when it is fixed and populates web page system fields
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfContentType(BlogPostPage.CONTENT_TYPE_NAME).WithContentTypeFields().WithWebPageData().WithLinkedItems(BlogPostPage.FullQueryDepth))
            .Parameters(p => p.Where(w => w.WhereIn(nameof(BlogPostPage.SystemFields.ContentItemGUID), Properties.SelectedBlogPosts.Select(i => i.Identifier))));
        var pages = await QueryExecutor.GetMappedWebPageResult<BlogPostPage>(b, new ContentQueryExecutionOptions { ForPreview = emailContext.BuilderMode != EmailBuilderMode.Off });
        Model = pages.TryFirst();
    }
}

public class BlogPostWidgetProperties : IEmailWidgetProperties
{
    [ContentItemSelectorComponent(
        BlogPostPage.CONTENT_TYPE_NAME,
        Label = "Selected blog post",
        AllowContentItemCreation = false,
        MinimumItems = 1,
        MaximumItems = 1,
        Order = 1)]
    public IEnumerable<ContentItemReference> SelectedBlogPosts { get; set; } = [];
}
