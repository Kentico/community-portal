using System.Reflection;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components.Widgets.Licenses;
using Kentico.Community.Portal.Web.Features.Blog.Events;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Features.Home;
using Kentico.Community.Portal.Web.Features.QAndA.Events;
using Kentico.Community.Portal.Web.Features.SEO;
using Kentico.Community.Portal.Web.Features.Support;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Infrastructure.Storage;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Community.Portal.Web.Rendering.Events;
using MediatR;
using Slugify;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionAppExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services, IConfiguration config) =>
        services
            .AddScoped<LicensesFacade>()
            .AddScoped<CookieConsentManager>()
            .AddScoped<ConsentManager>()
            .AddRendering()
            .AddSEO()
            .AddOperations(config)
            .AddInfrastructure(config)
            .AddSupport()
            .AddQAndA()
            .AddBlogs();

    private static IServiceCollection AddOperations(this IServiceCollection services, IConfiguration config) =>
        services.AddScoped<ICacheDependencyKeysBuilder, CacheDependencyKeysBuilder>()
            .Configure<DefaultQueryCacheSettings>(config.GetSection("Cache:Query"))
            .AddMediatR(c => c.RegisterServicesFromAssembly(typeof(HomePageQuery).Assembly))
            .AddClosedGenericTypes(typeof(HomePageQuery).Assembly, typeof(IQueryHandler<,>), ServiceLifetime.Scoped)
            .AddClosedGenericTypes(typeof(HomePageQuery).Assembly, typeof(ICommandHandler<,>), ServiceLifetime.Scoped)
            .Decorate(typeof(IRequestHandler<,>), typeof(QueryHandlerCacheDecorator<,>))
            .Decorate(typeof(ICommandHandler<,>), typeof(CommandHandlerLogDecorator<,>))
            .AddScoped<CacheDependenciesStore>()
            .AddScoped<ICacheDependenciesStore>(s => s.GetRequiredService<CacheDependenciesStore>())
            .AddScoped<ICacheDependenciesScope>(s => s.GetRequiredService<CacheDependenciesStore>())
            .AddTransient<WebPageCommandTools>()
            .AddTransient<WebPageQueryTools>()
            .AddTransient<ContentItemQueryTools>()
            .AddTransient<DataItemCommandTools>()
            .AddTransient<DataItemQueryTools>();

    private static IServiceCollection AddRendering(this IServiceCollection services) =>
        services
            .AddSingleton(s => new MarkdownRenderer())
            .AddSingleton<ISlugHelper>(_ => new SlugHelper(new SlugHelperConfiguration()))
            .AddScoped<ViewService>()
            .AddScoped<ClientAssets>();

    private static IServiceCollection AddSEO(this IServiceCollection services) =>
        services
            .AddScoped<WebPageMetaService>()
            .AddTransient<Sitemap>();

    private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config) =>
        services
            .AddSingleton<AzureStorageClientFactory>()
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<AssetItemService>()
            .AddTransient<MediaAssetContentMetadataHandler>()
            .AddScoped<CaptchaValidator>()
            .Configure<ReCaptchaSettings>(config.GetSection("ReCaptcha"));


    private static IServiceCollection AddSupport(this IServiceCollection services) =>
        services.AddHostedService<SupportMessageProcessorHostedService>();

    private static IServiceCollection AddQAndA(this IServiceCollection services) =>
        services.AddTransient<QAndAAnswerCreateSearchIndexTaskHandler>();
    private static IServiceCollection AddBlogs(this IServiceCollection services) =>
        services.AddTransient<BlogPostPublishCreateQAndAQuestionHandler>();

    private static IServiceCollection AddClosedGenericTypes(
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
