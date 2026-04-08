namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionUtilityExtensions
{
    public static IServiceCollection IfConfigured(this IServiceCollection services, IConfiguration config, string key, Action<IServiceCollection> configure)
    {
        if (config.GetValue<bool>(key))
        {
            configure(services);
        }

        return services;
    }

    public static IServiceCollection IfNotConfigured(this IServiceCollection services, IConfiguration config, string key, Action<IServiceCollection> configure)
    {
        if (!config.GetValue<bool>(key))
        {
            configure(services);
        }

        return services;
    }

}
