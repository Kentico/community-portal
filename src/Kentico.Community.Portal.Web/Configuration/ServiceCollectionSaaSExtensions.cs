using CMS.IO;
using Kentico.Xperience.Cloud;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionSaaSExtensions
{
    private const string LuceneStoragePath = "~/App_Data/LuceneSearch";
    private const string LuceneContainerName = "lucene";

    public static IServiceCollection AddAppXperienceSaaS(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        _ = services
            .AddStoragePathRegistration(LuceneStoragePath, PathType.SharedPersistent)
            .AddXperienceCloudStoragePathMapping(options =>
            {
                options.ConfigureContainerForPath = (registration, setup) =>
                {
                    if (string.Equals(registration.RegisteredPath, LuceneStoragePath, StringComparison.OrdinalIgnoreCase))
                    {
                        setup.ContainerName = LuceneContainerName;
                    }
                };
            });

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
