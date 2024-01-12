using System.Reflection;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components.ViewComponents.GTM;
using Kentico.Community.Portal.Web.Components.Widgets.Licenses;
using Kentico.Community.Portal.Web.Features.Blog.Events;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Features.Home;
using Kentico.Community.Portal.Web.Features.QAndA.Events;
using Kentico.Community.Portal.Web.Features.SEO;
using Kentico.Community.Portal.Web.Features.Support;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Community.Portal.Web.Rendering.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Slugify;
using Vite.AspNetCore.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionAppExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services, IConfiguration config) =>
        services
            .AddSingleton<IMemoryCache, MemoryCache>()
            .AddSingleton(s => new MarkdownRenderer())
            .AddSingleton<ISlugHelper>(_ => new SlugHelper(new SlugHelperConfiguration()))
            .AddScoped<ICacheDependencyKeysBuilder, CacheDependencyKeysBuilder>()
            .Configure<DefaultQueryCacheSettings>(s =>
            {
                var section = config.GetSection("Cache:Query");
                section.Bind(s);
            })
            .AddMediatR(c =>
            {
                _ = c.RegisterServicesFromAssembly(typeof(HomePageQuery).Assembly);
            })
            .AddClosedGenericTypes(typeof(HomePageQuery).Assembly, typeof(IQueryHandler<,>), ServiceLifetime.Scoped)
            .AddClosedGenericTypes(typeof(HomePageQuery).Assembly, typeof(ICommandHandler<,>), ServiceLifetime.Scoped)
            .Decorate(typeof(IRequestHandler<,>), typeof(QueryHandlerCacheDecorator<,>))
            .Decorate(typeof(ICommandHandler<,>), typeof(CommandHandlerLogDecorator<,>))
            .AddScoped<CacheDependenciesStore>()
            .AddScoped<ICacheDependenciesStore>(s => s.GetRequiredService<CacheDependenciesStore>())
            .AddScoped<ICacheDependenciesScope>(s => s.GetRequiredService<CacheDependenciesStore>())
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<AssetItemService>()
            .AddScoped<WebPageMetaService>()
            .AddScoped<CaptchaValidator>()
            .AddTransient<WebPageCommandTools>()
            .AddTransient<WebPageQueryTools>()
            .AddTransient<ContentItemQueryTools>()
            .AddTransient<DataItemCommandTools>()
            .AddTransient<DataItemQueryTools>()
            .AddScoped<ViewService>()
            .AddScoped<ClientAssets>()
            .AddScoped<SupportFacade>()
            .AddScoped<LicensesFacade>()
            .AddScoped<CookieConsentManager>()
            .AddScoped<ConsentManager>()
            .AddTransient<Sitemap>()
            .Configure<ReCaptchaSettings>(o =>
            {
                var section = config.GetSection("ReCaptcha");

                section.Bind(o);
            })
            .Configure<GoogleTagManagerSettings>(o =>
            {
                var section = config.GetSection("GoogleTagManager");

                section.Bind(o);
            })
            .Configure<MicrosoftDynamicsSettings>(o =>
            {
                var section = config.GetSection("MicrosoftDynamics");

                section.Bind(o);
            })
            .AddGlobalEventHandlers()
            .AddHttpClient()
            .AddViteServices();

    public static IServiceCollection AddGlobalEventHandlers(this IServiceCollection services) =>
        services
            .AddTransient<QAndAAnswerCreateSearchIndexTaskHandler>()
            .AddTransient<BlogPostPublishCreateQAndAQuestionHandler>()
            .AddTransient<MediaAssetContentMetadataHandler>();

    public static IServiceCollection AddClosedGenericTypes(
        this IServiceCollection services,
        Assembly assembly,
        Type typeToRegister,
        ServiceLifetime serviceLifetime)
    {
        _ = services.Scan(x => x.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeToRegister)
                .Where(t => !t.IsGenericType))
            .AsImplementedInterfaces()
            .WithLifetime(serviceLifetime));

        return services;
    }

}
