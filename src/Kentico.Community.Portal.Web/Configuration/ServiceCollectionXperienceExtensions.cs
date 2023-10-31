using CMS;
using Kentico.Activities.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.CrossSiteTracking.Web.Mvc;
using Kentico.Forms.Web.Mvc;
using Kentico.OnlineMarketing.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Localization.Routing;
using Kentico.Community.Portal.Web.Infrastructure;
using CMS.EmailEngine;
using Kentico.Community.Portal.Web.Components.Sections.Grid;
using Kentico.Community.Portal.Web.Features.DataCollection;
using CMS.Base.Configuration;

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
                    ContentTypeNames = new[]
                    {
                        HomePage.CONTENT_TYPE_NAME,
                        ResourceHubPage.CONTENT_TYPE_NAME,
                        QAndAQuestionPage.CONTENT_TYPE_NAME,
                        QAndANewQuestionPage.CONTENT_TYPE_NAME,
                        QAndALandingPage.CONTENT_TYPE_NAME,
                        BlogLandingPage.CONTENT_TYPE_NAME,
                        BlogPostPage.CONTENT_TYPE_NAME,
                        CommunityLandingPage.CONTENT_TYPE_NAME,
                        SupportPage.CONTENT_TYPE_NAME,
                        IntegrationsLandingPage.CONTENT_TYPE_NAME,
                        LandingPage.CONTENT_TYPE_NAME
                    }
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
            .Configure<EmailQueueOptions>(o =>
            {
                o.ArchiveDuration = 14;
            })
            .IfDevelopment(env, c =>
            {
                _ = c.Configure<SmtpOptions>(config.GetSection("SmtpOptions"))
                    .AddXperienceSmtp();
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
            .Configure<FormBuilderBundlesOptions>(options =>
            {
                options.JQueryCustomBundleWebRootPath = "Scripts/jquery-3.5.1.min.js";
                options.JQueryUnobtrusiveAjaxCustomBundleWebRootPath = "Scripts/jquery.unobtrusive-ajax.min.js";
            })
            .Configure<FileUploadOptions>(options =>
            {
                // No customization atm
            });
}
