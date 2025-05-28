using System.Text.Json;
using CMS.ContentEngine;
using CMS.MediaLibrary;
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

    public string ID { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime PublishedDate { get; set; }
    public List<string> DXTopicsFacets { get; set; } = [];
    public List<string> DXTopics { get; set; } = [];
    public string BlogTypeFacet { get; set; } = "";
    public string BlogType { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public int AuthorMemberID { get; set; }
    public string AuthorName { get; set; } = "";
    public ImageAssetViewModelSerializable? AuthorAvatarImage { get; set; } = null;

    public Document ToDocument()
    {
        var indexDocument = new Document()
        {
            new TextField(nameof(ID), ID, Field.Store.YES),
            new TextField(nameof(Title), Title, Field.Store.YES),
            new TextField(nameof(Content), Content, Field.Store.NO),
            new Int64Field(nameof(PublishedDate), DateTools.TicksToUnixTimeMilliseconds(PublishedDate.Ticks), Field.Store.YES),
            new TextField(nameof(DXTopics), string.Join(";", DXTopics), Field.Store.YES),
            new TextField(nameof(BlogType), BlogType, Field.Store.YES),
            new TextField(nameof(ShortDescription), ShortDescription, Field.Store.YES),
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
        var authorImage = JsonSerializer.Deserialize<ImageAssetViewModelSerializable>(doc.Get(nameof(AuthorAvatarImage)) ?? "{ }");

        var model = new BlogSearchIndexModel
        {
            ID = doc.Get(nameof(ID)),
            Url = doc.Get(nameof(Url)),
            Title = doc.Get(nameof(Title)),
            ShortDescription = doc.Get(nameof(ShortDescription)),
            DXTopics = doc.Get(nameof(DXTopics)).Split(";").WhereNotNullOrWhiteSpace().ToList(),
            BlogType = doc.Get(nameof(BlogType)),
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

public class BlogSearchIndexingStrategy(
    IContentQueryExecutor executor,
    WebScraperHtmlSanitizer htmlSanitizer,
    WebCrawlerService webCrawler,
    IMediator mediator) : DefaultLuceneIndexingStrategy
{
    public const string IDENTIFIER = "BLOG_SEARCH";

    private readonly IContentQueryExecutor executor = executor;
    private readonly WebScraperHtmlSanitizer htmlSanitizer = htmlSanitizer;
    private readonly WebCrawlerService webCrawler = webCrawler;
    private readonly IMediator mediator = mediator;

    public override Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem) =>
         Task.FromResult<IEnumerable<IIndexEventItemModel>>([changedItem]);

    public override Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem) =>
        Task.FromResult<IEnumerable<IIndexEventItemModel>>([changedItem]);

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

        var indexModel = new BlogSearchIndexModel
        {
            ID = page.SystemFields.WebPageItemGUID.ToString("N")
        };
        page
            .BlogPostPageAuthorContent
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

        var taxonomies = await mediator.Send(new BlogPostTaxonomiesQuery());

        page.BlogPostPageBlogType
            .TryFirst()
            .Map(t => t.Identifier)
            .Bind(id => taxonomies.Types
                .TryFirst(t => t.Guid == id))
            .Execute(tag =>
            {
                indexModel.BlogType = tag.DisplayName;
                indexModel.BlogTypeFacet = tag.NormalizedName;
            });
        var dxTopics = page
            .BlogPostPageDXTopics
            .Select(tagRef => taxonomies.DXTopicsAll
                .FirstOrDefault(t => tagRef.Identifier == t.Guid))
            .WhereNotNull();

        foreach (var tag in dxTopics)
        {
            indexModel.DXTopics.Add(tag.DisplayName);
            indexModel.DXTopicsFacets.Add(tag.NormalizedName);
        }

        string content = await webCrawler.CrawlWebPage(page);
        indexModel.Content = htmlSanitizer.SanitizeHtmlDocument(content);
        indexModel.Title = page.WebPageMetaTitle;
        indexModel.ShortDescription = page.WebPageMetaDescription;
        indexModel.PublishedDate = page.BlogPostPagePublishedDate != default
            ? page.BlogPostPagePublishedDate
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
        Dimensions = new() { Width = image.ImageContentAsset.Metadata.Width ?? 0, Height = image.ImageContentAsset.Metadata.Height ?? 0 };
    }

    public ImageAssetViewModelSerializable() { }

    public Guid ID { get; set; }
    public string Title { get; set; } = "";
    public string URL { get; set; } = "";
    public string AltText { get; set; } = "";
    public AssetDimensions Dimensions { get; set; } = new();

    public ImageViewModel ToImageViewModel() => new(Title, AltText, Dimensions.Width, Dimensions.Height, URL) { ID = ID };
}
