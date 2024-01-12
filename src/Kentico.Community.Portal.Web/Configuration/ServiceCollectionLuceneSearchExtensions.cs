using Kentico.Community.Portal.Web.Features.Blog.Models;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure.Search;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionLuceneSearchExtensions
{
    public static IServiceCollection AddAppLuceneSearch(this IServiceCollection services, IConfiguration config) =>
        services
            .AddLucene(builder =>
            {
                _ = builder
                    .RegisterStrategy<BlogSearchIndexingStrategy>(BlogSearchIndexingStrategy.IDENTIFIER)
                    .RegisterStrategy<QAndASearchIndexingStrategy>(QAndASearchIndexingStrategy.IDENTIFIER);
            })
            .AddSingleton<WebScraperHtmlSanitizer>()
            .AddHttpClient<WebCrawlerService>()
            .Services
            .AddSingleton<SearchService>()
            .Configure<LuceneSearchOptions>(config.GetSection("xperience.lucene"));
}
