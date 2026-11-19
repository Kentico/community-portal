using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Core.Membership;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.Members.EmailVerification;
using Kentico.Community.Portal.Web.Features.Testing;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Membership;
using Kentico.OnlineMarketing.Web.Mvc;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionMembershipExtensions
{
    public static IServiceCollection AddAppXperienceMembership(this IServiceCollection services) =>
        services
            // Sets the validation interval of members security stamp to zero so member's security stamp is validated with each request.
            .Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero)
            .AddAuthentication()
            .Services
            .AddIdentity<CommunityMember, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 10;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 1;

                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddDefaultTokenProviders()
            .AddUserStore<ApplicationUserStore<CommunityMember>>()
            .AddRoleStore<ApplicationRoleStore<ApplicationRole>>()
            .AddUserManager<UserManager<CommunityMember>>()
            .AddSignInManager<SignInManager<CommunityMember>>()
            .Services
            .AddScoped<MemberContactManager>()
            .AddScoped<MemberEmailVerificationService>()
            .AddScoped<IMemberEmailInUseChecker>(s => s.GetRequiredService<MemberEmailVerificationService>())
            .AddScoped<MemberEmailIntegrityHandler>()
            .AddScoped<RegistrationEmailConfirmationService>()
            .AddScoped<RecipientListManager>()
            .AddOptions<TermsOptions>()
            .BindConfiguration(TermsOptions.SECTION)
            .ValidateDataAnnotations()
            .ValidateOnStart()
            .Services
            .Decorate<IMemberToContactMapper, CommunityMemberToContactMapper>()
            .ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.LoginPath = new PathString("/login");
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddAuthorization()
            .AddSingleton<IMemberEmailService, MemberEmailService>()
            .AddScoped<E2EMemberModerationService>()
            .AddScoped<E2EMemberRoleService>();
}
