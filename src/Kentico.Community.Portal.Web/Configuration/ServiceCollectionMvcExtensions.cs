using System.Threading.RateLimiting;
using Kentico.Community.Portal.Web.Features.Errors;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Resources;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Vite.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionMvcExtensions
{
    public static IServiceCollection AddAppMvc(this IServiceCollection services, IWebHostEnvironment env, IConfiguration config) =>
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
            .AddServerSideBlazor() // Add Blazor services for component rendering
            .Services
            .AddSingleton<IHtmlGenerator, BootstrapHTMLGenerator>()
            .AddHttpContextAccessor()
            .AddHttpClient()
            .AddViteServices()
            .AddHealthChecks()
            .Services
            .AddRateLimitingOptions(config)
            .AddRateLimiter(options =>
            {
                // Limits come from RateLimitingOptions (appsettings "RateLimiting")
                _ = options
                    // Q&A content creation (authenticated)
                    .AddPolicy(QAndARateLimitingConstants.CreateQuestion, ctx => Partition(ctx, o => o.CreateQuestion))
                    .AddPolicy(QAndARateLimitingConstants.CreateAnswer, ctx => Partition(ctx, o => o.CreateAnswer))
                    .AddPolicy(QAndARateLimitingConstants.UpdateQuestion, ctx => Partition(ctx, o => o.UpdateQuestion))
                    .AddPolicy(QAndARateLimitingConstants.UpdateAnswer, ctx => Partition(ctx, o => o.UpdateAnswer))
                    .AddPolicy(QAndARateLimitingConstants.UpdateQuestionSubscription, ctx => Partition(ctx, o => o.UpdateQuestionSubscription))
                    .AddPolicy(QAndARateLimitingConstants.UpdateAnswerReaction, ctx => Partition(ctx, o => o.UpdateAnswerReaction))
                    .AddPolicy(QAndARateLimitingConstants.UpdateQuestionReaction, ctx => Partition(ctx, o => o.UpdateQuestionReaction))
                    // Authentication and member management (public)
                    .AddPolicy(MemberRateLimitingConstants.Login, ctx => Partition(ctx, o => o.Login))
                    .AddPolicy(MemberRateLimitingConstants.Registration, ctx => Partition(ctx, o => o.Registration))
                    .AddPolicy(MemberRateLimitingConstants.ForgotPassword, ctx => Partition(ctx, o => o.ForgotPassword))
                    .AddPolicy(MemberRateLimitingConstants.UpdateAvatar, ctx => Partition(ctx, o => o.UpdateAvatar))
                    .AddPolicy(MemberRateLimitingConstants.EmailVerification, ctx => Partition(ctx, o => o.EmailVerification))
                    .OnRejected = RateLimitingUtilities.OnRejectedHandler;
            });

    /// <summary>
    /// Binds <see cref="RateLimitingOptions" />. Unknown keys and invalid values are logged as errors at
    /// startup and keep the defaults (<see cref="RateLimitingOptionsSetup" />); the app still starts.
    /// </summary>
    public static IServiceCollection AddRateLimitingOptions(this IServiceCollection services, IConfiguration config) =>
        services
            .AddOptions()
            .AddSingleton<IConfigureOptions<RateLimitingOptions>>(sp =>
                new RateLimitingOptionsSetup(config, sp.GetRequiredService<ILogger<RateLimitingOptionsSetup>>()))
            .AddHostedService<RateLimitingOptionsStartupCheck>();

    private static RateLimitPartition<string> Partition(
        HttpContext httpContext,
        Func<RateLimitingOptions, RateLimitPolicyOptions> policy) =>
        RateLimitingUtilities.CreatePartition(
            httpContext,
            policy(httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value));
}
