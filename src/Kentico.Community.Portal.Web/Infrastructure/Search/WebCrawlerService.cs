using CMS.Core;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Kentico.Community.Portal.Web.Infrastructure.Search;

public class WebCrawlerService
{
    private readonly HttpClient httpClient;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IEventLogService eventLogService;

    public WebCrawlerService(HttpClient httpClient, IWebPageUrlRetriever urlRetriever, IEventLogService eventLogService, IOptions<LuceneSearchOptions> searchOptions)
    {
        this.httpClient = httpClient;
        this.httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "SearchCrawler");
        this.httpClient.BaseAddress = new Uri(searchOptions.Value.WebCrawlerBaseUrl);

        this.urlRetriever = urlRetriever;
        this.eventLogService = eventLogService;
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
            eventLogService.LogException(nameof(WebCrawlerService), nameof(CrawlWebPage), ex, $"Tree Path: {page.SystemFields.WebPageItemTreePath}");
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
            eventLogService.LogException(
                nameof(WebCrawlerService),
                nameof(CrawlPage),
                ex,
                $"Url: {url}");
        }
        return "";
    }
}
