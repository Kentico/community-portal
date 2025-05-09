using Kentico.Community.Portal.Web.Features.Errors;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Resources;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Vite.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionMvcExtensions
{
    public static IServiceCollection AddAppMvc(this IServiceCollection services, IWebHostEnvironment env) =>
        services
            .Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.AppendTrailingSlash = false;
                /*
                 * Must be proper case for token validation
                 * https://github.com/dotnet/aspnetcore/issues/40285#issuecomment-1047694858
                 */
                options.LowercaseQueryStrings = false;
            })
            .AddLocalization()
            /**
             * We aren't using Xperience's content localization yet,
             * so we rely on ASPNET Core's request localization which requires us to explicitly list all "supported" cultures
             * https://source.dot.net/#Microsoft.AspNetCore.Localization/RequestLocalizationMiddleware.cs,82
             * 
             * We're enabling the top UI cultures used by visitors to devnet.kentico.com over the previous 30 days from 2024/02/13
             * as reported by GA4
             *
             * We don't include _all_ cultures, because each culture entry costs memory and ASP.NET Core doesn't add
             * an "auto" culture feature because it could be a DoS vector
             * https://github.com/aspnet/Localization/issues/111#issuecomment-209539299
             */
            .Configure<RequestLocalizationOptions>(o =>
            {
                o.ApplyCurrentCultureToResponseHeaders = true;
                o.FallBackToParentUICultures = true;
                o.DefaultRequestCulture = new RequestCulture("en-US");
                o.SupportedUICultures ??= [];
                o.SupportedUICultures.AddRange(
                [
                    new("en"),
                    new("en-US"),
                    new("en-GB"),
                    new("cs"),
                    new("en-AU"),
                    new("cs-CZ"),
                    new("it"),
                    new("it-IT"),
                    new("tr"),
                    new("tr-TR"),
                    new("zh"),
                    new("zh-HK"),
                    new("nl"),
                    new("en-CA"),
                    new("en-IN"),
                    new("fr"),
                    new("fr-FR"),
                    new("de"),
                    new("de-DE"),
                    new("el"),
                    new("fa"),
                    new("fa-IR"),
                    new("sk"),
                    new("es"),
                    new("uk"),
                    new("uk-UA"),
                    new("ro"),
                    new("ro-RO"),
                ]);
            })
            .Configure<StaticFileOptions>(o =>
            {
                o.OnPrepareResponse = context =>
                {
                    // Caches static files for 7 days
                    context.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
                };
            })
            .AddControllersWithViews(config =>
            {
                if (!env.IsDevelopment())
                {
                    _ = config.Filters.Add<GlobalExceptionFilter>();
                }
            })
            .AddViewLocalization()
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider =
                    (type, factory) => factory.Create(typeof(SharedResources));
            })
            .Services
            .AddSingleton<IHtmlGenerator, BootstrapHTMLGenerator>()
            .AddHttpContextAccessor()
            .AddHttpClient()
            .AddViteServices()
            .AddHealthChecks()
            .Services;
}
