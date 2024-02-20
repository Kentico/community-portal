using Kentico.Web.Mvc;
using Kentico.Xperience.Cloud;
using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var config = builder.Configuration;

builder.Services
    // Customizes options configured via SaaS, so register this first
    .AddAppAdminAuthentication(config)
    .AddAppXperienceSaaS(config, env)
    .AddAppXperience(config, env)
    .AddAppXperienceMembership()
    .AddAppLuceneSearch(config)
    .AddAppMvc(env)
    .AddApp(config);

var app = builder.Build();

app
    .InitKentico()
    .IfDevelopment(env, b => b.UseDeveloperExceptionPage())
    .IfNotDevelopment(env, b => b.UseExceptionHandler("/error/500"))
    .UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = context =>
        {
            // Caches static files for 7 days
            context.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
        }
    })

    .UseCors()
    .UseCookiePolicy()
    .UseAuthentication()
    .UseKenticoCloud()
    .UseKentico()
    /**
     * By including this middleware after UseKentico() we can set the request culture/ui culture
     * based on something other than content localization and the URL.
     * The culture is used by the <see cref="Kentico.Community.Portal.Web.Rendering.ViewService.Culture"/>
     * https://source.dot.net/#Microsoft.AspNetCore.Localization/RequestLocalizationMiddleware.cs,110
     */
    .UseRequestLocalization()
    .UseStatusCodePagesWithReExecute("/error/{0}")
    .IfNotDevelopment(env, b => b.UseHsts())
    .UseAuthorization()
    .UseKenticoRoutes(app)
    .UseAppRoutes()
    .IfDevelopment(env, b => b.UseViteDevelopmentServer(true));

app.Run();
