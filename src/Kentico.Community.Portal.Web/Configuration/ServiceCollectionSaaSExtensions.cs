using Kentico.Xperience.Cloud;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionSaaSExtensions
{
    public static IServiceCollection AddAppXperienceSaaS(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        if (env.IsQa() || env.IsUat() || env.IsProduction())
        {
            _ = services
                .AddKenticoCloud(config)
                .AddXperienceCloudSendGrid(config)
                .AddXperienceCloudApplicationInsights(config);
        }

        return services;
    }
}
