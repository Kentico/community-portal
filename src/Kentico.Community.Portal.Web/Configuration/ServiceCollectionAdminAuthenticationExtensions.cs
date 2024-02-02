using Kentico.Membership;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionAdminAuthenticationExtensions
{
    public static IServiceCollection AddAppAdminAuthentication(this IServiceCollection services, IConfiguration config) =>
        services
            .AddAdminExternalAuthenticationProvider(
                authBuilder => authBuilder.AddMicrosoftIdentityWebApp(options =>
                {
                    var section = config.GetSection("CMSAdminSettings:Authentication:Identity:AzureAD");

                    section.Bind(options);

                    options.CallbackPath = new PathString(section.GetValue<string>("CallbackPath"));
                }),
                options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.AuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    options.UserSynchronizationFrequency = UserSynchronizationFrequency.Always;
                })
            .Configure<AdminIdentityOptions>(options =>
            {
                options.AuthenticationOptions.Mode = AdminAuthenticationMode.MaintainForms;
                options.AuthenticationOptions.ExpireTimeSpan = TimeSpan.FromHours(12);
            });
}
