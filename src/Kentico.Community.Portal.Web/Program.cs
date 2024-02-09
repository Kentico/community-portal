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
    .UseStaticFiles()
    .UseCors()
    .UseCookiePolicy()
    .UseAuthentication()
    .UseKenticoCloud()
    .UseKentico()
    .UseRequestLocalization()
    .UseStatusCodePagesWithReExecute("/error/{0}")
    .IfNotDevelopment(env, b => b.UseHsts())
    .UseAuthorization()
    .UseKenticoRoutes(app)
    .UseAppControllers()
    .IfDevelopment(env, b => b.UseViteDevelopmentServer(true));

app.Run();
