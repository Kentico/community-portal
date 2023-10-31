using System.Collections.Specialized;
using CMS.ContentEngine;
using CMS.Core;
using CMS.MediaLibrary;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Features.Blog.Models;

[IncludedPath("/", ContentTypes = new string[] {
    BlogPostPage.CONTENT_TYPE_NAME
})]
public class BlogSearchModel : LuceneSearchModel
{
    public const string IndexName = "BlogSearchModel";

    [TextField(true)]
    public string Title { get; set; } = "";
    [TextField(false)]
    public string Content { get; set; } = "";
    [Int64Field(true)]
    public long DateMilliseconds { get; set; }
    [TextField(true)]
    public string Taxonomy { get; set; } = "";
    [TextField(true)]
    public string ShortDescription { get; set; } = "";
    [TextField(true)]
    public string TeaserImageJSON { get; set; } = "{ }";

    [TextField(true)]
    public string AuthorName { get; set; } = "";
    [TextField(true)]
    public string AuthorAvatarImageJSON { get; set; } = "{ }";
    [TextField(true)]
    public string AuthorProfileLinkPath { get; set; } = "";

    [Int32Field(true)]
    public int ID { get; set; }

    public override IEnumerable<FacetField> OnTaxonomyFieldCreation()
    {
        if (!string.IsNullOrWhiteSpace(Taxonomy))
        {
            yield return new FacetField(nameof(Taxonomy), Taxonomy.ToLowerInvariant());
        }
    }
}

public class BlogSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public override async Task<LuceneSearchModel> OnIndexingNode(IndexedItemModel lucenePageItem, LuceneSearchModel model)
    {
        var mapper = Service.Resolve<IWebPageQueryResultMapper>();
        var executor = Service.Resolve<IContentQueryExecutor>();
        var assetService = Service.Resolve<AssetItemService>();
        var htmlSanitizer = Service.Resolve<WebScraperHtmlSanitizer>();
        var webCrawler = Service.Resolve<WebCrawlerService>();

        var blogModel = new BlogSearchModel
        {
            Url = model.Url,
            ObjectID = model.ObjectID
        };

        var b = new ContentItemQueryBuilder()
            .ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
            {
                _ = queryParameters
                    .WithLinkedItems(2)
                    .ForWebsite(lucenePageItem.ChannelName)
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemGUID), lucenePageItem.WebPageItemGuid));
            });

        var page = (await executor.GetWebPageResult(b, mapper.Map<BlogPostPage>)).FirstOrDefault();
        if (page is not null && page.BlogPostPageBlogPostContent.FirstOrDefault() is BlogPostContent blogPost)
        {
            await blogPost
                .BlogPostContentAuthor
                .TryFirst()
                .Execute(async author =>
                {
                    blogModel.AuthorName = author.FullName;
                    blogModel.AuthorProfileLinkPath = "";
                    var authorImg = await assetService.RetrieveMediaFileImage(author.AuthorContentPhotoMediaFileImage.FirstOrDefault());
                    blogModel.AuthorAvatarImageJSON = JsonConvert.SerializeObject(new ImageAssetViewModelSerializable(authorImg));
                });

            var blogImg = await assetService.RetrieveMediaFileImage(blogPost.BlogPostContentTeaserMediaFileImage.FirstOrDefault());
            blogModel.TeaserImageJSON = JsonConvert.SerializeObject(new ImageAssetViewModelSerializable(blogImg));

            string content = await webCrawler.CrawlWebPage(page);
            blogModel.Content = htmlSanitizer.SanitizeHtmlDocument(content);

            blogModel.Title = blogPost.BlogPostContentTitle;
            blogModel.Taxonomy = blogPost.BlogPostContentTaxonomy.ToLowerInvariant();
            blogModel.ShortDescription = blogPost.BlogPostContentShortDescription;

            var date = blogPost.BlogPostContentPublishedDate != default
                ? blogPost.BlogPostContentPublishedDate
                : DateTime.MinValue;

            blogModel.DateMilliseconds = DateTools.TicksToUnixTimeMilliseconds(date.Ticks);

            blogModel.ID = page.SystemFields.WebPageItemID;
        }

        return blogModel;
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(nameof(BlogSearchModel.Taxonomy), true);

        return facetConfig;
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
    public int PageSize => PAGE_SIZE;
}

public class BlogSearchResult
{
    public int ID { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Taxonomy { get; set; } = "";
    public string TeaserImageJSON { get; set; } = "{ }";
    public string AuthorName { get; set; } = "";
    public string AuthorAvatarImageJSON { get; set; } = "{ }";
    public string ShortDescription { get; set; } = "";
    public DateTime PublishedDate { get; set; }

    public static BlogSearchResult MapFromDocument(Document doc) => new()
    {
        ID = int.Parse(doc.Get(nameof(BlogSearchModel.ID))),
        Title = doc.Get(nameof(BlogSearchModel.Title)),
        Url = doc.Get(nameof(BlogSearchModel.Url)),
        ShortDescription = doc.Get(nameof(BlogSearchModel.ShortDescription)),
        Taxonomy = doc.Get(nameof(BlogSearchModel.Taxonomy)),
        TeaserImageJSON = doc.Get(nameof(BlogSearchModel.TeaserImageJSON)),
        AuthorName = doc.Get(nameof(BlogSearchModel.AuthorName)),
        AuthorAvatarImageJSON = doc.Get(nameof(BlogSearchModel.AuthorAvatarImageJSON)),
        PublishedDate = new DateTime(
            DateTools.UnixTimeMillisecondsToTicks(
                long.Parse(doc.Get(nameof(BlogSearchModel.DateMilliseconds)))
            ))
    };
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
    public string RelativePath { get; set; }
    public string DirectPath { get; set; }
    public NameValueCollection QueryStringParameters { get; set; }
    public bool IsImage { get; set; }
}
