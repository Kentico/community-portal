using Kentico.Community.Portal.Web.Features.Blog.Search;
using Kentico.Community.Portal.Web.Features.QAndA.Search;
using Kentico.Community.Portal.Web.Infrastructure.Search;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionLuceneSearchExtensions
{
    public static IServiceCollection AddAppLuceneSearch(this IServiceCollection services, IConfiguration config) =>
        services
            .AddKenticoLucene(builder =>
            {
                _ = builder
                    .RegisterStrategy<BlogSearchIndexingStrategy>(BlogSearchIndexingStrategy.IDENTIFIER)
                    .RegisterStrategy<QAndASearchIndexingStrategy>(QAndASearchIndexingStrategy.IDENTIFIER);
            }, config)
            .AddSingleton<WebScraperHtmlSanitizer>()
            .AddHttpClient<WebCrawlerService>()
            .Services
            .AddSingleton<BlogSearchService>()
            .AddSingleton<QAndASearchService>()
            .Configure<CommunityLuceneSearchOptions>(config.GetSection("Kentico.Xperience.Lucene.Custom"));
}
