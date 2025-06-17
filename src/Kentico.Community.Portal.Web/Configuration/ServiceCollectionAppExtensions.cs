using System.Reflection;
using CMS.DataEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Infrastructure;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Forms;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Licenses;
using Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;
using Kentico.Community.Portal.Web.Features.Blog.Events;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Features.QAndA.Events;
using Kentico.Community.Portal.Web.Features.SEO;
using Kentico.Community.Portal.Web.Features.Support;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Infrastructure.Storage;
using Kentico.Community.Portal.Web.Rendering;
using Sidio.Sitemap.AspNetCore;
using Sidio.Sitemap.Core.Services;
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
            .AddForms()
            .AddOperations(config)
            .AddInfrastructure(config)
            .AddSupport(config)
            .AddQAndA()
            .AddBlogs()
            .AddMemberBadges()
            .AddNotifications()
            .AddMigrations();

    private static IServiceCollection AddOperations(this IServiceCollection services, IConfiguration config) =>
        services
            .AddSingleton<ICacheDependencyKeysBuilder, CacheDependencyKeysBuilder>()
            .Configure<DefaultQueryCacheSettings>(settings =>
            {
                var section = config.GetSection("Cache:Query");

                settings.CacheItemDuration = TimeSpan.FromMinutes(section.GetValue<int>("CacheItemDuration"));
                settings.IsEnabled = section.GetValue<bool>("IsEnabled");
                settings.IsSlidingExpiration = section.GetValue<bool>("IsSlidingExpiration");
            })
            .AddMediatR(c => c
                .RegisterServicesFromAssembly(typeof(QAndAAnswerCountQuery).Assembly)
                .AddOpenBehavior(typeof(QueryCachingPipelineBehavior<,>))
                .AddOpenBehavior(typeof(CommandHandlerLogDecorator<,>)))
            .AddClosedGenericTypes(typeof(QAndAAnswerCountQuery).Assembly, typeof(IQueryHandler<,>), ServiceLifetime.Transient)
            .AddClosedGenericTypes(typeof(QAndAAnswerCountQuery).Assembly, typeof(ICommandHandler<,>), ServiceLifetime.Transient)
            .AddTransient<WebPageCommandTools>()
            .AddTransient<ContentItemManagerCreator>()
            .AddTransient<IContentQueryExecutionOptionsCreator, DefaultContentQueryExecutionOptionsCreator>()
            .AddTransient<WebPageQueryTools>()
            .AddTransient<ContentItemQueryTools>()
            .AddTransient<DataItemCommandTools>()
            .AddTransient<DataItemQueryTools>()
            .AddSingleton<IChannelDataProvider, ChannelDataProvider>()
            .AddTransient<AlertMessageCookieManager>();

    private static IServiceCollection AddRendering(this IServiceCollection services) =>
        services
            .AddSingleton(s => new MarkdownRenderer())
            .AddSingleton<ISlugHelper>(_ => new SlugHelper(new SlugHelperConfiguration()))
            .AddScoped<ViewService>()
            .AddScoped<AvatarImageService>()
            .AddScoped<ClientAssets>()
            .AddScoped<IJSEncoder, JSEncoder>()
            .AddScoped<IFormBuilderContext, FormBuilderContext>();

    private static IServiceCollection AddSEO(this IServiceCollection services) =>
        services
            .AddScoped<WebPageMetaService>()
            .AddTransient<SitemapRetriever>()
            .AddDefaultSitemapServices<HttpContextBaseUrlProvider>();

    private static IServiceCollection AddForms(this IServiceCollection services) =>
        services
            .AddScoped<IFormMemberEngagementRetriever, FormMemberEngagementRetriever>();

    private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config) =>
        services
            .AddSingleton(s => new ApplicationAssemblyInformation())
            .AddSingleton<AzureStorageClientFactory>()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<AssetItemService>()
            .AddSingleton<IStoragePathService, StoragePathService>()
            .AddScoped<CaptchaValidator>()
            .Configure<ReCaptchaSettings>(config.GetSection("ReCaptcha"));


    private static IServiceCollection AddSupport(this IServiceCollection services, IConfiguration config) =>
        services
            .AddHostedService<SupportRequestProcessorBackgroundService>()
            .AddSingleton<ISupportEmailSender, SupportEmailSender>()
            .Configure<SupportRequestProcessingSettings>(config.GetSection("SupportRequestProcessing"));

    private static IServiceCollection AddQAndA(this IServiceCollection services) =>
        services
            .AddInfoObjectEventHandler<InfoObjectBeforeInsertEvent<QAndAAnswerDataInfo>, QAndAAnswerSearchIndexTaskHandler>()
            .AddInfoObjectEventHandler<InfoObjectBeforeUpdateEvent<QAndAAnswerDataInfo>, QAndAAnswerSearchIndexTaskHandler>()
            .AddInfoObjectEventHandler<InfoObjectBeforeDeleteEvent<QAndAAnswerDataInfo>, QAndAAnswerSearchIndexTaskHandler>();
    private static IServiceCollection AddBlogs(this IServiceCollection services) =>
        services
            .AddTransient<BlogPostPublishCreateQAndAQuestionHandler>()
            .AddTransient<BlogPostContentAutoPopulateHandler>();

    private static IServiceCollection AddMemberBadges(this IServiceCollection services) =>
        services
            .AddSingleton<IMemberBadgeInfoProvider, MemberBadgeInfoProvider>()
            .AddSingleton<IMemberBadgeMemberInfoProvider, MemberBadgeMemberInfoProvider>()
            .AddTransient<MemberBadgeService>()
            .AddTransient<IMemberBadgeAssignmentRule, BlogPostAuthorMemberBadgeAssignmentRule>()
            // This rule is disabled because it has already run and no new members can qualify for it. It is left here as an example.
            //.AddTransient<IMemberBadgeAssignmentRule, EarlyAdopterMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, DiscussionAuthorMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, DiscussionParticipantMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, DiscussionAcceptedAnswerAuthorMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, KenticoEmployeeMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, IntegrationAuthorMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, MemberProfileLevel1BadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, CommunityContentContributorMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, MemberProfileLevel1BadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, MemberAnniversary1YearMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, MemberAnniversary2YearMemberBadgeAssignmentRule>()
            .AddTransient<IMemberBadgeAssignmentRule, MemberAnniversary3YearMemberBadgeAssignmentRule>()
            .AddHostedService<MemberBadgeAssignmentApplicationBackgroundService>();

    private static IServiceCollection AddNotifications(this IServiceCollection services) =>
        services;

    private static IServiceCollection AddMigrations(this IServiceCollection services) =>
        services;

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
