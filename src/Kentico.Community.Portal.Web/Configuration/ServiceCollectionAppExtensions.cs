using System.Reflection;
using CMS.Base;
using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Infrastructure;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Forms;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Heading;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Licenses;
using Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;
using Kentico.Community.Portal.Web.Features.Blog.Events;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Features.DataProtection;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Features.Members.Events;
using Kentico.Community.Portal.Web.Features.Newsletter;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Features.QAndA.Events;
using Kentico.Community.Portal.Web.Features.QAndA.Notifications;
using Kentico.Community.Portal.Web.Features.SEO;
using Kentico.Community.Portal.Web.Features.SEO.IndexNow;
using Kentico.Community.Portal.Web.Features.SEO.SocialImages;
using Kentico.Community.Portal.Web.Features.SEO.StructuredData;
using Kentico.Community.Portal.Web.Features.Testing;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Sidio.Sitemap.AspNetCore;
using Sidio.Sitemap.Core.Services;
using Slugify;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionAppExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services, IConfiguration config, IHostEnvironment env) =>
        services
            .AddScoped<CookieConsentManager>()
            .AddSingleton<ICurrentContactAccessor, CurrentContactAccessor>()
            .AddTransient<ITagHelperComponent, ConsentModeTagHelperComponent>()
            .AddScoped<ConsentRetriever>()
            .AddPrivacyTesting()
            .AddE2EContactCorrelation(config, env)
            .AddRendering()
            .AddSEO()
            .AddIndexNow(config)
            .AddForms()
            .AddOperations(config)
            .AddInfrastructure(config)
            .AddLicenses()
            .AddQAndA()
            .AddBlogs()
            .AddMembers()
            .AddMemberBadges()
            .AddQAndADiscussionNotifications()
            .AddNewsletter(config)
            .AddDataProtection(config)
            .AddMcp(config);

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
            .AddScoped<IHeadingContext, HeadingContext>()
            .AddScoped<ViewService>()
            .AddScoped<DateTimeDisplayService>()
            .AddScoped<MemberAssetContentService>()
            .AddScoped<AvatarImageService>()
            .AddScoped<ClientAssets>()
            .AddScoped<IJSEncoder, JSEncoder>()
            .AddScoped<IFormBuilderContext, FormBuilderContext>()
            .AddScoped<IRazorComponentRenderer, RazorComponentRenderer>()
            .AddScoped<IPageBuilderContext, PageBuilderContext>()
            .AddSingleton<IWidgetPropertiesSerializer, WidgetPropertiesSerializer>();

    private static IServiceCollection AddSEO(this IServiceCollection services) =>
        services
            .AddScoped<WebPageMetaService>()
            .AddScoped<JsonLdCollector>()
            .AddTransient<SitemapRetriever>()
            .AddTransient<LlmsTxtRetriever>()
            .AddSingleton<ISocialImageGenerator>(s => new MagickSocialImageGenerator(s.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider))
            .AddDefaultSitemapServices<HttpContextBaseUrlProvider>();

    /// <summary>
    /// Publish events only queue pages; <see cref="IndexNowSubmissionService"/> submits them out of band.
    /// Disabled until <c>IndexNow:Key</c> is set.
    /// </summary>
    private static IServiceCollection AddIndexNow(this IServiceCollection services, IConfiguration config)
    {
        _ = services
            .Configure<IndexNowOptions>(config.GetSection(IndexNowOptions.SECTION))
            .AddSingleton<IndexNowQueue>()
            .AddTransient<IndexNowUrlResolver>()
            .AddEventHandler<AfterPublishWebPageEvent, IndexNowPublishHandler>()
            .AddHostedService<IndexNowSubmissionService>()
            .AddHttpClient<IndexNowClient>();

        return services;
    }

    /// <summary>
    /// Registered unconditionally, like E2EMemberModerationService - the endpoints that use them are
    /// only mapped when E2ETestingGate allows (Testing:E2E:Enabled in Development or CI; startup fails elsewhere).
    /// </summary>
    private static IServiceCollection AddPrivacyTesting(this IServiceCollection services) =>
        services
            .AddScoped<E2EPrivacyTestingService>()
            .AddScoped<E2EScheduledTaskRunner>()
            .AddScoped<E2ENewsletterTestingService>()
            .AddScoped<E2ETagManagerStub>()
            .AddScoped<E2EReadOnlyModeSwitch>()
            .AddScoped<E2EDataProtectionTestingService>();

    private static IServiceCollection AddNewsletter(this IServiceCollection services, IConfiguration config) =>
        services
            .Configure<EmailMarketingOptions>(config.GetSection(EmailMarketingOptions.SECTION))
            // Transient: scheduled tasks resolve from the root provider, so scoped services fail there
            .AddTransient<EmailMarketingConsentManager>()
            .AddTransient<EmailMarketingAgreementHandler>();

    /// <summary>
    /// Transient for the same reason as the newsletter services: scheduled tasks resolve from the
    /// root provider. The Data protection adapters create a scope per call.
    /// </summary>
    private static IServiceCollection AddDataProtection(this IServiceCollection services, IConfiguration config) =>
        services
            .Configure<RetentionOptions>(config.GetSection(RetentionOptions.SECTION))
            .AddTransient<CommunityFormDataService>()
            .AddTransient<MemberContentItemsQuery>()
            .AddTransient<MemberAnonymizer>()
            .AddTransient<CommunityPersonalDataCollectorService>()
            .AddTransient<CommunityPersonalDataEraserService>();

    private static IServiceCollection AddForms(this IServiceCollection services) =>
        services
            .AddScoped<IFormMemberEngagementRetriever, FormMemberEngagementRetriever>();

    private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config) =>
        services
            .AddSingleton(s => new ApplicationAssemblyInformation())
            .AddSingleton(TimeProvider.System)
            .AddScoped<CaptchaValidator>()
            .AddTransient<AdminLinkBuilder>()
            .Configure<ReCaptchaSettings>(config.GetSection("ReCaptcha"))
            .AddSingleton<IObjectDisplayOptionsProvider, DefaultObjectDisplayOptionsProvider>()
            .AddSingleton<IObjectsRetriever, CustomObjectsRetriever>();

    private static IServiceCollection AddLicenses(this IServiceCollection services) =>
        services
            .AddTransient<ILicenseFileService, LicenseFileService>();

    private static IServiceCollection AddQAndA(this IServiceCollection services) =>
        services
            .AddTransient<IQAndAPermissionService, QAndAPermissionService>()
            .AddInfoObjectEventHandler<InfoObjectBeforeInsertEvent<QAndAAnswerDataInfo>, QAndAAnswerSearchIndexTaskHandler>()
            .AddInfoObjectEventHandler<InfoObjectBeforeUpdateEvent<QAndAAnswerDataInfo>, QAndAAnswerSearchIndexTaskHandler>()
            .AddInfoObjectEventHandler<InfoObjectBeforeDeleteEvent<QAndAAnswerDataInfo>, QAndAAnswerSearchIndexTaskHandler>();
    private static IServiceCollection AddBlogs(this IServiceCollection services) =>
        services
            .AddTransient<BlogPostPublishCreateQAndAQuestionHandler>()
            .AddTransient<BlogPostContentAutoPopulateHandler>()
            // Async handler: runs after the publish and never throws (failures are logged)
            .AddEventHandler<AfterPublishWebPageEvent, BlogPostPublishSocialImageHandler>();

    private static IServiceCollection AddMembers(this IServiceCollection services) =>
        services
            .AddTransient<KenticoEmployeeOffboardingHandler>();

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
            .AddTransient<IMemberBadgeAssignmentRule, MemberAnniversary3YearMemberBadgeAssignmentRule>();

    private static IServiceCollection AddQAndADiscussionNotifications(this IServiceCollection services) =>
        services
            .AddTransient<QAndANotificationsProcessor>()
            .AddTransient<QAndANotificationLogger>()
            .AddTransient<QAndANotificationSettingsManager>();

    private static IServiceCollection AddMcp(this IServiceCollection services, IConfiguration config)
    {
        if (!config.GetValue<bool>("Kentico:VirtualInbox:Enabled"))
        {
            return services;
        }

        _ = services
            .AddComponentRegistryMcpServices()
            .AddMcpServer()
            .WithHttpTransport()
            .WithComponentRegistryTools()
            .WithVirtualInboxTools();

        return services;
    }

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
