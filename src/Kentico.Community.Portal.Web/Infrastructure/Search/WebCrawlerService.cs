using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Kentico.Community.Portal.Web.Infrastructure.Search;

public class WebCrawlerService
{
    private readonly HttpClient httpClient;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly ILogger<WebCrawlerService> logger;

    public WebCrawlerService(HttpClient httpClient, IWebPageUrlRetriever urlRetriever, ILogger<WebCrawlerService> logger, IOptions<CommunityLuceneSearchOptions> searchOptions)
    {
        this.httpClient = httpClient;
        this.httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "SearchCrawler");
        this.httpClient.BaseAddress = new Uri(searchOptions.Value.WebCrawlerBaseUrl);

        this.urlRetriever = urlRetriever;
        this.logger = logger;
    }

    public async Task<string> CrawlWebPage(IWebPageFieldsSource page)
    {
        try
        {
            var url = await urlRetriever.Retrieve(page);
            string path = url.RelativePath.TrimStart('~').TrimStart('/');

            return await CrawlPage(path);
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, "WEB_CRAWL_FAILURE"), ex, "Failed to crawl web page with tree path {TreePath}", page.SystemFields.WebPageItemTreePath);
        }
        return "";
    }

    public async Task<string> CrawlPage(string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, "WEB_CRAWL_PAGE_FAILURE"), ex, "Failed to crawl url {Url}", url);
        }
        return "";
    }
}
