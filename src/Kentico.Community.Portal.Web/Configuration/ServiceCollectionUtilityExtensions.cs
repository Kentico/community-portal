namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionUtilityExtensions
{
    public static IServiceCollection IfDevelopment(this IServiceCollection services, IWebHostEnvironment env, Action<IServiceCollection> configure)
    {
        if (env.IsDevelopment())
        {
            configure(services);
        }

        return services;
    }

    public static IServiceCollection IfNotDevelopment(this IServiceCollection services, IWebHostEnvironment env, Action<IServiceCollection> configure)
    {
        if (!env.IsDevelopment())
        {
            configure(services);
        }

        return services;
    }
}
