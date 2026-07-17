using CMS.IO;
using Kentico.Xperience.Cloud;
using Kentico.Xperience.Lucene.Core.Store;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionSaaSExtensions
{
    public static IServiceCollection AddAppXperienceSaaS(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        _ = services
            .AddStoragePathRegistration($"~/{LuceneStorageConstants.LUCENE_INDEX_PATH}/", PathType.SharedPersistent)
            .AddXperienceCloudStoragePathMapping();

        if (env.IsQa() || env.IsUat() || env.IsProduction())
        {
            _ = services
                .AddKenticoCloud(config)
                .AddXperienceCloudSendGrid(config)
                .AddXperienceCloudApplicationInsights(config)
                .AddXperienceCloudDataProtection(config);
        }

        return services;
    }
}
