using System.Text.Json;
using CMS.ContentEngine;
using CMS.Core;
using CMS.MediaLibrary;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Xperience.Lucene.Core.Indexing;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Facet;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.Blog.Search;

public class BlogSearchIndexModel
{
    public const string IndexName = "BLOG_SEARCH";

    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime PublishedDate { get; set; }
    public List<string> DXTopicsFacets { get; set; } = [];
    public List<string> DXTopics { get; set; } = [];
    public string BlogTypeFacet { get; set; } = "";
    public string BlogType { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public ImageAssetViewModelSerializable? TeaserImage { get; set; } = null;
    public int AuthorMemberID { get; set; }
    public string AuthorName { get; set; } = "";
    public ImageAssetViewModelSerializable? AuthorAvatarImage { get; set; } = null;

    public Document ToDocument()
    {
        var indexDocument = new Document()
        {
            new TextField(nameof(Title), Title, Field.Store.YES),
            new TextField(nameof(Content), Content, Field.Store.NO),
            new Int64Field(nameof(PublishedDate), DateTools.TicksToUnixTimeMilliseconds(PublishedDate.Ticks), Field.Store.YES),
            new TextField(nameof(DXTopics), string.Join(";", DXTopics), Field.Store.YES),
            new TextField(nameof(BlogType), BlogType, Field.Store.YES),
            new TextField(nameof(ShortDescription), ShortDescription, Field.Store.YES),
            new TextField(nameof(TeaserImage), JsonSerializer.Serialize(TeaserImage), Field.Store.YES),
            new Int32Field(nameof(AuthorMemberID), AuthorMemberID, Field.Store.YES),
            new TextField(nameof(AuthorName), AuthorName, Field.Store.YES),
            new TextField(nameof(AuthorAvatarImage), JsonSerializer.Serialize(AuthorAvatarImage), Field.Store.YES),
        };

        foreach (string topicFacet in DXTopicsFacets)
        {
            _ = indexDocument.AddFacetField(nameof(DXTopicsFacets), topicFacet);
        }

        if (!string.IsNullOrWhiteSpace(BlogTypeFacet))
        {
            _ = indexDocument.AddFacetField(nameof(BlogTypeFacet), BlogTypeFacet);
        }

        return indexDocument;
    }

    public static BlogSearchIndexModel FromDocument(Document doc)
    {
        var teaserImage = JsonSerializer.Deserialize<ImageAssetViewModelSerializable>(doc.Get(nameof(TeaserImage)) ?? "{ }");
        var authorImage = JsonSerializer.Deserialize<ImageAssetViewModelSerializable>(doc.Get(nameof(AuthorAvatarImage)) ?? "{ }");

        var model = new BlogSearchIndexModel
        {
            Url = doc.Get(nameof(Url)),
            Title = doc.Get(nameof(Title)),
            ShortDescription = doc.Get(nameof(ShortDescription)),
            DXTopics = [.. doc.Get(nameof(DXTopics)).Split(";")],
            BlogType = doc.Get(nameof(BlogType)),
            TeaserImage = teaserImage,
            AuthorMemberID = int.TryParse(doc.Get(nameof(AuthorMemberID)), out int authorMemberID)
                ? authorMemberID
                : 0,
            AuthorName = doc.Get(nameof(AuthorName)),
            AuthorAvatarImage = authorImage,
            PublishedDate = new DateTime(
                DateTools.UnixTimeMillisecondsToTicks(
                    long.Parse(doc.Get(nameof(PublishedDate)))
                ))
        };

        return model;
    }
}

public partial class BlogSearchIndexingStrategy(
    IContentQueryExecutor executor,
    WebScraperHtmlSanitizer htmlSanitizer,
    WebCrawlerService webCrawler,
    IChannelDataProvider channelDataProvider,
    IMediator mediator,
    IEventLogService log) : DefaultLuceneIndexingStrategy
{
    public const string IDENTIFIER = "BLOG_SEARCH";

    private readonly IContentQueryExecutor executor = executor;
    private readonly WebScraperHtmlSanitizer htmlSanitizer = htmlSanitizer;
    private readonly WebCrawlerService webCrawler = webCrawler;
    private readonly IChannelDataProvider channelDataProvider = channelDataProvider;
    private readonly IMediator mediator = mediator;
    private readonly IEventLogService log = log;

    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem) => await Task.FromResult<List<IIndexEventItemModel>>([changedItem]);

    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem)
    {
        if (string.Equals(changedItem.ContentTypeName, BlogPostContent.CONTENT_TYPE_NAME))
        {
            var reindexable = new List<IIndexEventItemModel>();

            /*
            * We only need the values required for the IndexedItemModel, which come
            * from the BlogPostPage web page item that links to the updated BlogPostContent content item
            */
            var b = new ContentItemQueryBuilder()
                .ForContentTypes(q =>
                    q.OfContentType(BlogPostPage.CONTENT_TYPE_NAME)
                        .ForWebsite(true)
                        .Linking(BlogPostPage.CONTENT_TYPE_NAME, nameof(BlogPostPage.BlogPostPageBlogPostContent), [changedItem.ItemID]));

            var page = (await executor.GetMappedWebPageResult<IWebPageFieldsSource>(b)).FirstOrDefault();
            if (page is null)
            {
                log.LogWarning(
                    source: nameof(FindItemsToReindex),
                    eventCode: "MISSING_BLOGPOSTPAGE",
                    eventDescription: $"Could not find blog web site page for blog content [{changedItem.ItemID}].{Environment.NewLine}Skipping search indexing.");

                return reindexable;
            }

            string channelName = await channelDataProvider.GetChannelNameByWebsiteChannelID(page.SystemFields.WebPageItemWebsiteChannelId) ?? "";

            reindexable.Add(new IndexEventWebPageItemModel(
                page.SystemFields.WebPageItemID,
                page.SystemFields.WebPageItemGUID,
                changedItem.LanguageName,
                BlogPostPage.CONTENT_TYPE_NAME,
                page.SystemFields.WebPageItemName,
                page.SystemFields.ContentItemIsSecured,
                page.SystemFields.ContentItemContentTypeID,
                page.SystemFields.ContentItemCommonDataContentLanguageID,
                channelName,
                page.SystemFields.WebPageItemTreePath,
                page.SystemFields.WebPageItemParentID,
                page.SystemFields.WebPageItemOrder));

            return reindexable;
        }

        return [];
    }

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        if (item is not IndexEventWebPageItemModel webpageItem || !string.Equals(item.ContentTypeName, BlogPostPage.CONTENT_TYPE_NAME))
        {
            return null;
        }

        var b = new ContentItemQueryBuilder()
            .ForWebPage(
                BlogPostPage.CONTENT_TYPE_NAME,
                webpageItem.ItemGuid,
                q => q.WithLinkedItems(BlogPostPage.FullQueryDepth));

        var page = (await executor.GetMappedWebPageResult<BlogPostPage>(b)).FirstOrDefault();
        if (page is null)
        {
            return null;
        }
        if (page.BlogPostPageBlogPostContent.FirstOrDefault() is not BlogPostContent blogPost)
        {
            return null;
        }

        var indexModel = new BlogSearchIndexModel();

        blogPost
            .BlogPostContentAuthor
            .TryFirst()
            .Execute(author =>
            {
                indexModel.AuthorMemberID = author.AuthorContentMemberID;
                indexModel.AuthorName = author.FullName;
                author.AuthorContentPhotoImageContent
                    .TryFirst()
                    .Map(i => new ImageAssetViewModelSerializable(i))
                    .Execute(i => indexModel.AuthorAvatarImage = i);
            });

        blogPost.ListableItemFeaturedImageContent
            .TryFirst()
            .Map(i => new ImageAssetViewModelSerializable(i))
            .Execute(i => indexModel.TeaserImage = i);

        var taxonomies = await mediator.Send(new BlogPostTaxonomiesQuery());

        blogPost.BlogPostContentBlogType
            .TryFirst()
            .Map(t => t.Identifier)
            .Bind(id => taxonomies.Types
                .TryFirst(t => t.Guid == id))
            .Execute(tag =>
            {
                indexModel.BlogType = tag.DisplayName;
                indexModel.BlogTypeFacet = tag.NormalizedName;
            });
        var dxTopics = blogPost
            .BlogPostContentDXTopics
            .Select(tagRef => taxonomies.DXTopics
                .FirstOrDefault(t => tagRef.Identifier == t.Guid))
            .WhereNotNull();

        foreach (var tag in dxTopics)
        {
            indexModel.DXTopics.Add(tag.DisplayName);
            indexModel.DXTopicsFacets.Add(tag.NormalizedName);
        }

        string content = await webCrawler.CrawlWebPage(page);
        indexModel.Content = htmlSanitizer.SanitizeHtmlDocument(content);
        indexModel.Title = string.IsNullOrWhiteSpace(blogPost.ListableItemTitle)
            ? blogPost.ListableItemTitle
            : blogPost.ListableItemTitle;
        indexModel.ShortDescription = string.IsNullOrWhiteSpace(blogPost.ListableItemShortDescription)
            ? blogPost.ListableItemShortDescription
            : blogPost.ListableItemShortDescription;
        indexModel.PublishedDate = blogPost.BlogPostContentPublishedDate != default
            ? blogPost.BlogPostContentPublishedDate
            : DateTime.MinValue;

        return indexModel.ToDocument();
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(nameof(BlogSearchIndexModel.DXTopicsFacets), true);
        facetConfig.SetMultiValued(nameof(BlogSearchIndexModel.BlogTypeFacet), false);

        return facetConfig;
    }
}

public class ImageAssetViewModelSerializable
{
    public ImageAssetViewModelSerializable(ImageContent image)
    {
        ID = image.SystemFields.ContentItemGUID;
        Title = image.MediaItemTitle;
        URL = image.ImageContentAsset.Url;
        AltText = image.MediaItemShortDescription;
        Dimensions = new() { Width = image.MediaItemAssetWidth, Height = image.MediaItemAssetHeight };
    }

    public ImageAssetViewModelSerializable() { }

    public Guid ID { get; set; }
    public string Title { get; set; } = "";
    public string URL { get; set; } = "";
    public string AltText { get; set; } = "";
    public AssetDimensions Dimensions { get; set; } = new();

    public ImageViewModel ToImageViewModel() => new(Title, AltText, Dimensions.Width, Dimensions.Height, URL) { ID = ID };
}
