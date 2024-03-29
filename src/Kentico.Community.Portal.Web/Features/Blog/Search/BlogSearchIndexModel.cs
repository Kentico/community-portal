using System.Collections.Specialized;
using CMS.ContentEngine;
using CMS.Core;
using CMS.MediaLibrary;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Indexing;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Facet;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Features.Blog.Search;

public class BlogSearchIndexModel
{
    public const string IndexName = "BLOG_SEARCH";

    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime PublishedDate { get; set; }
    public const string TaxonomyFacetField = $"{nameof(Taxonomy)}_Facet";
    public string Taxonomy { get; set; } = "";
    public const string BlogTypeFacetField = $"{nameof(BlogType)}_Facet";
    public string BlogType { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public ImageAssetViewModelSerializable? TeaserImage { get; set; } = null;
    public int AuthorMemberID { get; set; }
    public string AuthorName { get; set; } = "";
    public ImageAssetViewModelSerializable? AuthorAvatarImage { get; set; } = null;
    public string AuthorProfileLinkPath { get; set; } = "";

    public Document ToDocument()
    {
        var indexDocument = new Document()
        {
            new TextField(nameof(Title), Title, Field.Store.YES),
            new TextField(nameof(Content), Content, Field.Store.NO),
            new Int64Field(nameof(PublishedDate), DateTools.TicksToUnixTimeMilliseconds(PublishedDate.Ticks), Field.Store.YES),
            new TextField(nameof(Taxonomy), (string.IsNullOrWhiteSpace(Taxonomy) ? "untaxonomized" : Taxonomy).ToLowerInvariant(), Field.Store.YES),
            new TextField(nameof(BlogType), (string.IsNullOrWhiteSpace(BlogType) ? "untaxonomized" : BlogType).ToLowerInvariant(), Field.Store.YES),
            new TextField(nameof(ShortDescription), ShortDescription, Field.Store.YES),
            new TextField(nameof(TeaserImage), JsonConvert.SerializeObject(TeaserImage), Field.Store.YES),
            new Int32Field(nameof(AuthorMemberID), AuthorMemberID, Field.Store.YES),
            new TextField(nameof(AuthorName), AuthorName, Field.Store.YES),
            new TextField(nameof(AuthorAvatarImage), JsonConvert.SerializeObject(AuthorAvatarImage), Field.Store.YES),
            new TextField(nameof(AuthorProfileLinkPath), AuthorProfileLinkPath, Field.Store.YES),
        };

        _ = indexDocument.AddFacetField(nameof(TaxonomyFacetField), indexDocument.Get(nameof(Taxonomy)));
        _ = indexDocument.AddFacetField(nameof(BlogTypeFacetField), indexDocument.Get(nameof(BlogType)));

        return indexDocument;
    }

    public static BlogSearchIndexModel FromDocument(Document doc)
    {
        var teaserImage = JsonConvert.DeserializeObject<ImageAssetViewModelSerializable>(doc.Get(nameof(TeaserImage)) ?? "{ }");
        var authorImage = JsonConvert.DeserializeObject<ImageAssetViewModelSerializable>(doc.Get(nameof(AuthorAvatarImage)) ?? "{ }");

        var model = new BlogSearchIndexModel
        {
            Url = doc.Get(nameof(Url)),
            Title = doc.Get(nameof(Title)),
            ShortDescription = doc.Get(nameof(ShortDescription)),
            Taxonomy = doc.Get(nameof(Taxonomy)),
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

public class BlogSearchIndexingStrategy(
    IContentQueryExecutor executor,
    AssetItemService assetService,
    ITaxonomyRetriever taxonomyRetriever,
    WebScraperHtmlSanitizer htmlSanitizer,
    WebCrawlerService webCrawler,
    IEventLogService log) : DefaultLuceneIndexingStrategy
{
    public const string IDENTIFIER = "BLOG_SEARCH";

    private readonly IContentQueryExecutor executor = executor;
    private readonly AssetItemService assetService = assetService;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;
    private readonly WebScraperHtmlSanitizer htmlSanitizer = htmlSanitizer;
    private readonly WebCrawlerService webCrawler = webCrawler;
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
                        .ForWebsite(false)
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

            reindexable.Add(new IndexEventWebPageItemModel(
                page.SystemFields.WebPageItemID,
                page.SystemFields.WebPageItemGUID,
                changedItem.LanguageName,
                BlogPostPage.CONTENT_TYPE_NAME,
                page.SystemFields.WebPageItemName,
                page.SystemFields.ContentItemIsSecured,
                page.SystemFields.ContentItemContentTypeID,
                page.SystemFields.ContentItemCommonDataContentLanguageID,
                /**
                 * We set the channel name to an empty string because we can't retrieve it from the query above
                 * and do not use it in MapToLuceneDocumentOrNull
                 */
                "",
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
            .ForWebPage(BlogPostPage.CONTENT_TYPE_NAME, webpageItem.ItemGuid, queryParameters => queryParameters.WithLinkedItems(2));

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

        await blogPost
            .BlogPostContentAuthor
            .TryFirst()
            .Execute(async author =>
            {
                indexModel.AuthorMemberID = author.AuthorContentMemberID;
                indexModel.AuthorName = author.FullName;
                indexModel.AuthorProfileLinkPath = "";
                var authorImg = await assetService.RetrieveMediaFileImage(author.AuthorContentPhotoMediaFileImage.FirstOrDefault());
                if (authorImg is not null)
                {
                    indexModel.AuthorAvatarImage = new ImageAssetViewModelSerializable(authorImg);
                }
            });

        var blogImg = await assetService.RetrieveMediaFileImage(blogPost.BlogPostContentTeaserMediaFileImage.FirstOrDefault());
        if (blogImg is not null)
        {
            indexModel.TeaserImage = new ImageAssetViewModelSerializable(blogImg);
        }

        var tag = (await taxonomyRetriever.RetrieveTags(blogPost.BlogPostContentBlogType.Select(t => t.Identifier), item.LanguageName)).FirstOrDefault();

        string content = await webCrawler.CrawlWebPage(page);
        indexModel.Content = htmlSanitizer.SanitizeHtmlDocument(content);
        indexModel.Title = blogPost.BlogPostContentTitle;
        indexModel.Taxonomy = blogPost.BlogPostContentTaxonomy;
        if (tag?.Title is string tagTitle)
        {
            indexModel.BlogType = tagTitle;
        }
        indexModel.ShortDescription = blogPost.BlogPostContentShortDescription;
        indexModel.PublishedDate = blogPost.BlogPostContentPublishedDate != default
            ? blogPost.BlogPostContentPublishedDate
            : DateTime.MinValue;

        return indexModel.ToDocument();
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(nameof(BlogSearchIndexModel.TaxonomyFacetField), false);
        facetConfig.SetMultiValued(nameof(BlogSearchIndexModel.BlogTypeFacetField), false);

        return facetConfig;
    }
}



public class ImageAssetViewModelSerializable
{
    public ImageAssetViewModelSerializable(ImageAssetViewModel imageAsset)
    {
        ID = imageAsset.ID;
        Title = imageAsset.Title;
        URLData = new SerializableMediaFileUrl(imageAsset.URLData);
        AltText = imageAsset.AltText;
        Dimensions = imageAsset.Dimensions;
        Extension = imageAsset.Extension;
    }

    public ImageAssetViewModelSerializable() { }

    public Guid ID { get; set; }
    public string Title { get; set; } = "";
    public SerializableMediaFileUrl URLData { get; set; } = new();
    public string AltText { get; set; } = "";
    public AssetDimensions Dimensions { get; set; } = new();
    public string Extension { get; set; } = "";

    public ImageAssetViewModel ToImageAsset() =>
        new(ID, Title, URLData.ToMediaFileUrl(), AltText, Dimensions, Extension);
}

public class SerializableMediaFileUrl
{
    public string RelativePath { get; set; } = "";
    public string DirectPath { get; set; } = "";
    public bool IsImage { get; set; }

    public SerializableMediaFileUrl(IMediaFileUrl url)
    {
        RelativePath = url.RelativePath;
        DirectPath = url.DirectPath;
        IsImage = url.IsImage;
    }

    public SerializableMediaFileUrl() { }

    public DefaultMediaFileUrl ToMediaFileUrl() =>
        new()
        {
            DirectPath = DirectPath,
            IsImage = IsImage,
            RelativePath = RelativePath,
            QueryStringParameters = []
        };
}

public class DefaultMediaFileUrl : IMediaFileUrl
{
    public string RelativePath { get; set; } = "";
    public string DirectPath { get; set; } = "";
    public NameValueCollection QueryStringParameters { get; set; } = [];
    public bool IsImage { get; set; }
}
