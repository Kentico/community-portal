using Kentico.Web.Mvc;

namespace Microsoft.AspNetCore.Builder;

public static class ApplicationBuilderUtilityExtensions
{
    public static IApplicationBuilder IfDevelopment(this IApplicationBuilder app, IWebHostEnvironment env, Action<IApplicationBuilder> configure)
    {
        if (env.IsDevelopment())
        {
            configure(app);
        }

        return app;
    }

    public static IApplicationBuilder IfNotDevelopment(this IApplicationBuilder app, IWebHostEnvironment env, Action<IApplicationBuilder> configure)
    {
        if (!env.IsDevelopment())
        {
            configure(app);
        }

        return app;
    }

    /// <summary>
    /// Adds MiniProfiler to the middleware pipeline based on environment and configuration
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="app"></param>
    /// <param name="config"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static WebApplication UseAppMiniProfiler(this IApplicationBuilder builder, WebApplication app, IConfiguration config, IWebHostEnvironment env)
    {
        if (config.GetSection("Kentico.Xperience.MiniProfiler.Custom").GetValue<bool>("IsEnabled"))
        {
            _ = builder.IfDevelopment(env, b => b.UseMiniProfiler());
        }

        return app;
    }

    public static WebApplication UseKenticoRoutes(this IApplicationBuilder _, WebApplication app)
    {
        app.Kentico().MapRoutes();

        return app;
    }

    public static WebApplication UseAppRoutes(this WebApplication app)
    {
        _ = app.MapControllers();

        _ = app.MapHealthChecks("status");

        return app;
    }
}
