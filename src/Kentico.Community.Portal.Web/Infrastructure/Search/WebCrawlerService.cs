using CMS.Core;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Kentico.Community.Portal.Web.Infrastructure.Search;

public class WebCrawlerService
{
    private readonly HttpClient httpClient;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IEventLogService log;

    public WebCrawlerService(HttpClient httpClient, IWebPageUrlRetriever urlRetriever, IEventLogService log, IOptions<LuceneSearchOptions> searchOptions)
    {
        this.httpClient = httpClient;
        this.httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "SearchCrawler");
        this.httpClient.BaseAddress = new Uri(searchOptions.Value.WebCrawlerBaseUrl);

        this.urlRetriever = urlRetriever;
        this.log = log;
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
            log.LogException(nameof(WebCrawlerService), nameof(CrawlWebPage), ex, $"Tree Path: {page.SystemFields.WebPageItemTreePath}");
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
            log.LogException(
                nameof(WebCrawlerService),
                nameof(CrawlPage),
                ex,
                $"Url: {url}");
        }
        return "";
    }
}
