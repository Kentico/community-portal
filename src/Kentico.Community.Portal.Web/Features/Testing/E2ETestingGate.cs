namespace Kentico.Community.Portal.Web.Features.Testing;

/// <summary>
/// The one place that decides whether E2E testing tools (the <c>/testing/e2e</c> endpoints and
/// E2E-only services) are on. Every E2E registration must call <see cref="IsEnabled" /> so the
/// endpoints and their services can't drift apart.
/// </summary>
/// <remarks>
/// On only when <see cref="ENABLED_KEY" /> is true AND the environment is Development or
/// <see cref="CI_ENVIRONMENT" />. The endpoints export and erase personal data, grant roles and
/// inject scripts, so the flag set anywhere else (Production, qa, uat...) is a misconfiguration
/// and stops the app at startup instead of exposing them.
/// </remarks>
public static class E2ETestingGate
{
    public const string ENABLED_KEY = "Testing:E2E:Enabled";

    /// <summary>ASPNETCORE_ENVIRONMENT in the CI workflow and the Portal.WebCI launch profile.</summary>
    public const string CI_ENVIRONMENT = "CI";

    /// <exception cref="InvalidOperationException">
    /// <see cref="ENABLED_KEY" /> is true outside Development and CI.
    /// </exception>
    public static bool IsEnabled(IConfiguration config, IHostEnvironment env)
    {
        if (!config.GetValue<bool>(ENABLED_KEY))
        {
            return false;
        }

        if (env.IsDevelopment() || env.IsEnvironment(CI_ENVIRONMENT))
        {
            return true;
        }

        throw new InvalidOperationException(
            $"{ENABLED_KEY} is true in the '{env.EnvironmentName}' environment. E2E testing endpoints are only allowed in Development and {CI_ENVIRONMENT}. Remove the setting from this environment's configuration.");
    }
}
