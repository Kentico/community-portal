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
