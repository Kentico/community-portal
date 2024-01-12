using System.Collections.Specialized;
using CMS.ContentEngine;
using CMS.Core;
using CMS.MediaLibrary;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Indexing;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Features.Blog.Models;

public class BlogSearchModel
{
    public const string IndexName = "BLOG_SEARCH";

    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime PublishedDate { get; set; }
    public string Taxonomy { get; set; } = "";
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
            new FacetField(nameof(Taxonomy), string.IsNullOrWhiteSpace(Taxonomy) ? "Untaxonomized" : Taxonomy),
            new TextField(nameof(ShortDescription), ShortDescription, Field.Store.YES),
            new TextField(nameof(TeaserImage), JsonConvert.SerializeObject(TeaserImage), Field.Store.YES),
            new Int32Field(nameof(AuthorMemberID), AuthorMemberID, Field.Store.YES),
            new TextField(nameof(AuthorName), AuthorName, Field.Store.YES),
            new TextField(nameof(AuthorAvatarImage), JsonConvert.SerializeObject(AuthorAvatarImage), Field.Store.YES),
            new TextField(nameof(AuthorProfileLinkPath), AuthorProfileLinkPath, Field.Store.YES),
        };

        return indexDocument;
    }

    public static BlogSearchModel FromDocument(Document doc)
    {
        var teaserImage = JsonConvert.DeserializeObject<ImageAssetViewModelSerializable>(doc.Get(nameof(TeaserImage)) ?? "{ }");
        var authorImage = JsonConvert.DeserializeObject<ImageAssetViewModelSerializable>(doc.Get(nameof(AuthorAvatarImage)) ?? "{ }");

        var model = new BlogSearchModel
        {
            Url = doc.Get(nameof(Url)),
            Title = doc.Get(nameof(Title)),
            ShortDescription = doc.Get(nameof(ShortDescription)),
            Taxonomy = doc.Get(nameof(Taxonomy)),
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

public class BlogSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public const string IDENTIFIER = "BLOG_SEARCH";

    // TODO - temporary until more advanced web page querying is available : https://roadmap.kentico.com/c/193-new-api-cross-content-type-querying
    public const string WEBSITE_CHANNEL_NAME = "devnet";
    private readonly IContentQueryExecutor executor;
    private readonly IWebPageQueryResultMapper mapper;
    private readonly AssetItemService assetService;
    private readonly WebScraperHtmlSanitizer htmlSanitizer;
    private readonly WebCrawlerService webCrawler;
    private readonly IEventLogService log;

    public BlogSearchIndexingStrategy(
        IContentQueryExecutor executor,
        IWebPageQueryResultMapper mapper,
        AssetItemService assetService,
        WebScraperHtmlSanitizer htmlSanitizer,
        WebCrawlerService webCrawler,
        IEventLogService log)
    {
        this.executor = executor;
        this.mapper = mapper;
        this.assetService = assetService;
        this.htmlSanitizer = htmlSanitizer;
        this.webCrawler = webCrawler;
        this.log = log;
    }

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        if (item is not IndexEventWebPageItemModel webpageItem || !string.Equals(item.ContentTypeName, BlogPostPage.CONTENT_TYPE_NAME))
        {
            return null;
        }

        var blogModel = new BlogSearchModel();

        var b = new ContentItemQueryBuilder()
            .ForWebPage(webpageItem.WebsiteChannelName, BlogPostPage.CONTENT_TYPE_NAME, webpageItem.ItemGuid, queryParameters => queryParameters.WithLinkedItems(2));

        var page = (await executor.GetWebPageResult(b, mapper.Map<BlogPostPage>)).FirstOrDefault();
        if (page is not null && page.BlogPostPageBlogPostContent.FirstOrDefault() is BlogPostContent blogPost)
        {
            await blogPost
                .BlogPostContentAuthor
                .TryFirst()
                .Execute(async author =>
                {
                    blogModel.AuthorMemberID = author.AuthorContentMemberID;
                    blogModel.AuthorName = author.FullName;
                    blogModel.AuthorProfileLinkPath = "";
                    var authorImg = await assetService.RetrieveMediaFileImage(author.AuthorContentPhotoMediaFileImage.FirstOrDefault());
                    if (authorImg is not null)
                    {
                        blogModel.AuthorAvatarImage = new ImageAssetViewModelSerializable(authorImg);
                    }
                });

            var blogImg = await assetService.RetrieveMediaFileImage(blogPost.BlogPostContentTeaserMediaFileImage.FirstOrDefault());
            if (blogImg is not null)
            {
                blogModel.TeaserImage = new ImageAssetViewModelSerializable(blogImg);
            }

            string content = await webCrawler.CrawlWebPage(page);
            blogModel.Content = htmlSanitizer.SanitizeHtmlDocument(content);
            blogModel.Title = blogPost.BlogPostContentTitle;
            blogModel.Taxonomy = blogPost.BlogPostContentTaxonomy.ToLowerInvariant();
            blogModel.ShortDescription = blogPost.BlogPostContentShortDescription;
            blogModel.PublishedDate = blogPost.BlogPostContentPublishedDate != default
                ? blogPost.BlogPostContentPublishedDate
                : DateTime.MinValue;
        }

        return blogModel.ToDocument();
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(nameof(BlogSearchModel.Taxonomy), false);

        return facetConfig;
    }

    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem) => await Task.FromResult(new List<IIndexEventItemModel>() { changedItem });

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
                .ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
                {
                    _ = queryParameters
                        .ForWebsite(WEBSITE_CHANNEL_NAME)
                        .Linking(nameof(BlogPostPage.BlogPostPageBlogPostContent), new[] { changedItem.ItemID });
                });

            var page = (await executor.GetWebPageResult(b, mapper.Map<BlogPostPage>)).FirstOrDefault();
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
                WEBSITE_CHANNEL_NAME,
                page.SystemFields.WebPageItemTreePath,
                page.SystemFields.WebPageItemParentID,
                page.SystemFields.WebPageItemOrder));

            return reindexable;
        }

        return Enumerable.Empty<IIndexEventItemModel>();
    }
}

public class BlogSearchRequest
{
    public const int PAGE_SIZE = 10;

    public BlogSearchRequest(HttpRequest request)
    {
        var query = request.Query;

        Facet = query.TryGetValue("facet", out var facetValues)
            ? facetValues.ToString()
            : "";
        SearchText = query.TryGetValue("query", out var queryValues)
            ? queryValues.ToString()
            : "";
        SortBy = query.TryGetValue("sortBy", out var sortByValues)
            ? sortByValues.ToString()
            : "date";
        PageNumber = query.TryGetValue("page", out var pageValues)
            ? int.TryParse(pageValues, out int p)
                ? p
                : 1
            : 1;
    }

    public BlogSearchRequest(string sortBy, int pageSize)
    {
        SortBy = sortBy;
        PageSize = pageSize;
    }

    public void Deconstruct(out string searchText, out string facet, out string sortBy, out int pageNumber, out int pageSize)
    {
        searchText = SearchText;
        facet = Facet;
        sortBy = SortBy;
        pageNumber = PageNumber;
        pageSize = PageSize;
    }

    public string Facet { get; } = "";
    public string SearchText { get; } = "";
    public string SortBy { get; } = "";
    public int PageNumber { get; } = 1;
    public int AuthorMemberID { get; set; }
    public int PageSize { get; } = PAGE_SIZE;

    public bool AreFiltersDefault => string.IsNullOrWhiteSpace(SearchText) && AuthorMemberID < 1;
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
            QueryStringParameters = new NameValueCollection()
        };
}

public class DefaultMediaFileUrl : IMediaFileUrl
{
    public string RelativePath { get; set; } = "";
    public string DirectPath { get; set; } = "";
    public NameValueCollection QueryStringParameters { get; set; } = new();
    public bool IsImage { get; set; }
}
