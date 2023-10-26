using Kentico.Web.Mvc;
using Kentico.Xperience.Cloud;
using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var config = builder.Configuration;

builder.Services
    .AddAppXperienceMembership()
    .AddAppXperienceSaaS(config, env)
    .AddAppXperience(config, env)
    .AddAppAdminAuthentication(config)
    .AddAppLuceneSearch(config)
    .AddAppMvc(env)
    .AddApp(config);

var app = builder.Build();

app
    .InitKentico()
    .IfDevelopment(env, b => b.UseDeveloperExceptionPage())
    .UseStaticFiles()
    .UseCors()
    .UseCookiePolicy()
    .UseAuthentication()
    .UseKenticoCloud()
    .UseKentico()
    .UseStatusCodePagesWithReExecute("/error/{0}")
    .IfNotDevelopment(env, b => b.UseHsts())
    .UseAuthorization()
    .UseKenticoRoutes(app)
    .UseAppControllers()
    .IfDevelopment(env, b => b.UseViteDevMiddleware());

app.Run();
