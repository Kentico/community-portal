using Kentico.Community.Portal.Web.Features.Blog.Models;
using Kentico.Community.Portal.Web.Features.QAndA;

//using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure.Search;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Analysis.Standard;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionLuceneSearchExtensions
{
    public static IServiceCollection AddAppLuceneSearch(this IServiceCollection services, IConfiguration config)
    {
        var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

        return services
            .AddLucene(new[]
            {
                new LuceneIndex(
                    typeof(BlogSearchModel),
                    analyzer,
                    BlogSearchModel.IndexName,
                    "devnet",
                    "en-US",
                    luceneIndexingStrategy: new BlogSearchIndexingStrategy()),
                new LuceneIndex(
                    typeof(QAndASearchModel),
                    analyzer,
                    QAndASearchModel.IndexName,
                    "devnet",
                    "en-US",
                    luceneIndexingStrategy: new QAndASearchIndexingStrategy())
            })
            .AddSingleton<WebScraperHtmlSanitizer>()
            .AddHttpClient<WebCrawlerService>()
            .Services
            .AddSingleton<SearchService>()
            .Configure<LuceneSearchOptions>(config.GetSection("xperience.lucene"));
    }
}
