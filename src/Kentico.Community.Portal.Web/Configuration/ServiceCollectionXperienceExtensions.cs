using CMS.Base;
using CMS.Base.Configuration;
using CMS.ContentEngine;
using CMS.ContentSynchronization;
using CMS.Core;
using CMS.EmailEngine;
using CMS.OnlineForms;
using CMS.Scheduler.Internal;
using Kentico.Activities.Web.Mvc;
using Kentico.Community.Portal.Core.Emails;
using Kentico.Community.Portal.Web.Components.EmailBuilder.Sections.SingleColumn;
using Kentico.Community.Portal.Web.Components.PageBuilder.Sections.Grid;
using Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.OnlineMarketing.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Kentico.Xperience.Labs.ComponentRegistry;
using Kentico.Xperience.Labs.ContentModelGraph.Admin;
using Kentico.Xperience.ManagementApi;
using Kentico.Xperience.Mjml;
using Kentico.Xperience.TagManager.Rendering;
using Microsoft.AspNetCore.Localization.Routing;
using XperienceCommunity.Compare;
using XperienceCommunity.FormClone;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionXperienceExtensions
{
    public static IServiceCollection AddAppXperience(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env) =>
        services
            .AddKentico(features =>
            {
                features.UsePageBuilder(new PageBuilderOptions
                {
                    DefaultSectionIdentifier = GridSection.IDENTIFIER,
                    RegisterDefaultSection = false,
                    ContentTypeNames =
                    [
                        HomePage.CONTENT_TYPE_NAME,
                        ResourceHubPage.CONTENT_TYPE_NAME,
                        QAndAQuestionPage.CONTENT_TYPE_NAME,
                        QAndANewQuestionPage.CONTENT_TYPE_NAME,
                        QAndALandingPage.CONTENT_TYPE_NAME,
                        BlogLandingPage.CONTENT_TYPE_NAME,
                        BlogPostPage.CONTENT_TYPE_NAME,
                        CommunityLandingPage.CONTENT_TYPE_NAME,
                        CommunityLinksPage.CONTENT_TYPE_NAME,
                        SupportPage.CONTENT_TYPE_NAME,
                        IntegrationsLandingPage.CONTENT_TYPE_NAME,
                        LandingPage.CONTENT_TYPE_NAME
                    ]
                });

                features.UseEmailMarketing();
                features.UseEmailStatisticsLogging();
                features.UseEmailBuilder();

                features.UseActivityTracking();

                features.UseWebPageRouting();
            })
            .Configure<EmailBuilderOptions>(options =>
            {
                options.AllowedEmailContentTypeNames =
                [
                    AutoresponderEmail.CONTENT_TYPE_NAME,
                    NewsletterEmail.CONTENT_TYPE_NAME
                ];
                options.RegisterDefaultSection = false;
                options.DefaultSectionIdentifier = SingleColumnSection.IDENTIFIER;
            })
            .AddMjmlForEmails()
            .AddKenticoManagementApiIfDevelopment(config, env)
            .AddKenticoTagManager(config)
            .AddPreviewComponentOutlines()
            .AddComponentRegistry()
            .AddContentModelGraph()
            .AddXperienceCommunityFormClone()
            .AddXperienceCompare()
            .AddVirtualInboxServices(config)
            .AddMiniProfiler(config)
            .Configure<EmailQueueOptions>(config.GetSection("EmailQueueOptions"))
            // Only Development/CI set this (default 30s); it delays automation Wait steps
            .Configure<SchedulerOptions>(config.GetSection("SchedulerOptions"))
            .AddContentSynchronization(config)
            .Configure<CookieLevelOptions>(options =>
            {
                options.CookieConfigurations.Add(CookieNames.CONSENT, CookieLevel.System);
                // Essential, so alert dismissals are remembered without tracking consent
                options.CookieConfigurations.Add(AlertMessageCookieManager.COOKIE_NAME_NOTIFICATION_CLEARED, CookieLevel.Essential);
            })
            .Configure<KenticoRequestLocalizationOptions>(options =>
            {
                options.RequestCultureProviders.Add(new RouteDataRequestCultureProvider
                {
                    RouteDataStringKey = "culture",
                    UIRouteDataStringKey = "culture"
                });
            })
            .Configure<FileUploadOptions>(options =>
            {
                // 500 MB
                options.MaxFileSize = 524_288_000L;
                // 20 MB
                options.ChunkSize = 20_971_520;
            })
            .Configure<ContentRetrieverCacheOptions>(options =>
            {
                options.DefaultCacheExpiration = TimeSpan.FromMinutes(10);
            })
            .Configure<ReadOnlyModeOptions>(config.GetSection("ReadOnlyModeOptions"))
            .AddSingleton<FormFieldTextValueExtractorBase<DateTime>, DateTimeFormFieldTextValueExtractor>()
            .AddSingleton<IFormFieldContentItemReferenceExtractor, MarkdownContentItemReferenceExtractor>()
            .AddSingleton<IEmailActivityTrackingEvaluator, ConsentEmailActivityTrackingEvaluator>()
            .Decorate<ILocalizationService, ApplicationLocalizationService>()
            // No Tag Manager snippet renders until the visitor allows Analytics
            .Configure<TagManagerConsentOptions>(config.GetSection(TagManagerConsentOptions.SECTION))
            .Decorate<IChannelCodeSnippetsService, ConsentGatedCodeSnippetsService>();

    private static IServiceCollection AddKenticoManagementApiIfDevelopment(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            return services;
        }

        string secret = config["Kentico.Xperience.ManagementApi:Secret"] ?? string.Empty;

        return services.AddKenticoManagementApi(options => options.Secret = secret);
    }

    private static IServiceCollection AddVirtualInboxServices(this IServiceCollection services, IConfiguration config) =>
        services
            .IfConfigured(config, "Kentico:VirtualInbox:Enabled", s =>
            {
                _ = s.AddVirtualInboxClient(config)
                    .Configure<SystemEmailOptions>(config.GetSection("SystemEmailOptions"));
            })
            .IfNotConfigured(config, "Kentico:VirtualInbox:Enabled", s => _ = s.AddVirtualInboxCore());

    private static IServiceCollection AddMiniProfiler(this IServiceCollection services, IConfiguration config) =>
        services.IfConfigured(config, "Kentico.Xperience.MiniProfiler.Custom:IsEnabled", s => _ = s.AddKenticoMiniProfiler());

    private static IServiceCollection AddContentSynchronization(this IServiceCollection services, IConfiguration config) =>
        services.Configure<ContentSynchronizationOptions>(config.GetSection("ContentSynchronizationOptions"));
}
