using CMS;
using CMS.Base.Configuration;
using CMS.ContentEngine;
using CMS.Core;
using CMS.EmailEngine;
using Kentico.Activities.Web.Mvc;
using Kentico.Community.Portal.Web.Components.Sections.Grid;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Infrastructure.Storage;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.CrossSiteTracking.Web.Mvc;
using Kentico.OnlineMarketing.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Localization.Routing;

[assembly: RegisterModule(typeof(StorageInitializationModule))]

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

                features.UseActivityTracking();

                features.UseCrossSiteTracking(new CrossSiteTrackingOptions
                {
                    ConsentSettings = null
                });

                features.UseWebPageRouting();
            })
            .AddKenticoTagManager(config)
            .AddPreviewComponentOutlines()
            .Configure<EmailQueueOptions>(o =>
            {
                o.ArchiveDuration = 14;
            })
            .IfDevelopment(env, c =>
            {
                _ = c.Configure<SmtpOptions>(config.GetSection("SmtpOptions"))
                    .AddXperienceSmtp()
                    .Configure<SystemEmailOptions>(config.GetSection("SystemEmailOptions"));

                if (config.GetSection("Kentico.Xperience.MiniProfiler.Custom").GetValue<bool>("IsEnabled"))
                {
                    _ = c.AddKenticoMiniProfiler();
                }
            })
            .Configure<CookieLevelOptions>(options =>
            {
                options.CookieConfigurations.Add(CookieNames.COOKIE_CONSENT_LEVEL, CookieLevel.System);
                options.CookieConfigurations.Add(CookieNames.COOKIE_ACCEPTANCE, CookieLevel.System);
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
            .AddSingleton<IContentItemReferenceExtractor, MarkdownContentItemReferenceExtractor>()
            .AddSingleton<IEmailActivityTrackingEvaluator, ConsentEmailActivityTrackingEvaluator>()
            .Decorate<ILocalizationService, ApplicationLocalizationService>();
}
