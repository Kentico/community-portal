using System.Reflection;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Spam/abuse limits for each rate limiting policy. Bound from the <c>RateLimiting</c> section.
/// Partitioned per member (signed in) or per client IP (anonymous).
/// </summary>
/// <remarks>
/// Defaults are the production values. Override only the values you need, for example:
/// <code>
/// "RateLimiting": {
///   "Registration": { "PermitLimit": 10, "Window": "00:05:00" },
///   "CreateAnswer": { "PermitLimit": 2, "Window": "00:01:00", "SegmentsPerWindow": 2 }
/// }
/// </code>
/// <list type="bullet">
/// <item><c>PermitLimit</c>: requests allowed per <c>Window</c>.</item>
/// <item><c>Window</c>: <c>hh:mm:ss</c>.</item>
/// <item><c>SegmentsPerWindow</c>: sliding window segments. Omit/null = fixed window.</item>
/// </list>
/// Keys: <c>Login</c>, <c>Registration</c>, <c>ForgotPassword</c>, <c>UpdateAvatar</c>,
/// <c>CreateQuestion</c>, <c>CreateAnswer</c>, <c>UpdateQuestion</c>, <c>UpdateAnswer</c>,
/// <c>UpdateQuestionSubscription</c>, <c>UpdateQuestionReaction</c>, <c>UpdateAnswerReaction</c>.
/// Changes need an app restart. Unknown keys and invalid values are logged as errors at startup
/// and the policy keeps its default (<see cref="RateLimitingOptionsSetup" />).
/// </remarks>
public class RateLimitingOptions
{
    public const string SECTION = "RateLimiting";

    /// <summary>Member sign-in. Default 50 / 10 min, fixed.</summary>
    public RateLimitPolicyOptions Login { get; set; } = new(50, TimeSpan.FromMinutes(10));

    /// <summary>Member registration. Default 10 / 5 min, fixed.</summary>
    public RateLimitPolicyOptions Registration { get; set; } = new(10, TimeSpan.FromMinutes(5));

    /// <summary>Password recovery requests. Default 10 / 30 min, fixed.</summary>
    public RateLimitPolicyOptions ForgotPassword { get; set; } = new(10, TimeSpan.FromMinutes(30));

    /// <summary>Avatar uploads. Default 5 / 5 min, sliding (5 segments).</summary>
    public RateLimitPolicyOptions UpdateAvatar { get; set; } = new(5, TimeSpan.FromMinutes(5), 5);

    /// <summary>Email change and alternate email requests that send a verification email. Default 5 / 30 min, fixed.</summary>
    public RateLimitPolicyOptions EmailVerification { get; set; } = new(5, TimeSpan.FromMinutes(30));

    /// <summary>New Q&amp;A questions. Default 1 / 4 min, sliding (4 segments).</summary>
    public RateLimitPolicyOptions CreateQuestion { get; set; } = new(1, TimeSpan.FromMinutes(4), 4);

    /// <summary>New Q&amp;A answers. Default 2 / 1 min, sliding (2 segments).</summary>
    public RateLimitPolicyOptions CreateAnswer { get; set; } = new(2, TimeSpan.FromMinutes(1), 2);

    /// <summary>Q&amp;A question edits. Default 10 / 5 min, sliding (5 segments).</summary>
    public RateLimitPolicyOptions UpdateQuestion { get; set; } = new(10, TimeSpan.FromMinutes(5), 5);

    /// <summary>Q&amp;A answer edits. Default 10 / 5 min, sliding (5 segments).</summary>
    public RateLimitPolicyOptions UpdateAnswer { get; set; } = new(10, TimeSpan.FromMinutes(5), 5);

    /// <summary>Q&amp;A question subscribe/unsubscribe. Default 10 / 2 min, sliding (5 segments).</summary>
    public RateLimitPolicyOptions UpdateQuestionSubscription { get; set; } = new(10, TimeSpan.FromMinutes(2), 5);

    /// <summary>Q&amp;A question reactions. Default 10 / 1 min, sliding (10 segments).</summary>
    public RateLimitPolicyOptions UpdateQuestionReaction { get; set; } = new(10, TimeSpan.FromMinutes(1), 10);

    /// <summary>Q&amp;A answer reactions. Default 10 / 1 min, sliding (10 segments).</summary>
    public RateLimitPolicyOptions UpdateAnswerReaction { get; set; } = new(10, TimeSpan.FromMinutes(1), 10);
}

/// <summary>
/// One policy's limit. <see cref="SegmentsPerWindow"/> null = fixed window, else sliding window.
/// </summary>
public class RateLimitPolicyOptions
{
    public RateLimitPolicyOptions() { }

    public RateLimitPolicyOptions(int permitLimit, TimeSpan window, int? segmentsPerWindow = null)
    {
        PermitLimit = permitLimit;
        Window = window;
        SegmentsPerWindow = segmentsPerWindow;
    }

    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
    public int? SegmentsPerWindow { get; set; }
}


/// <summary>
/// Binds <see cref="RateLimitingOptions" /> one policy at a time. A bad config never stops the app or
/// breaks a request: each problem is logged as an error (event log) and the policy keeps its default.
/// <list type="bullet">
/// <item>Unknown keys (a misspelled key would otherwise silently keep the default).</item>
/// <item>Values that don't parse (e.g. <c>"Window": "5m"</c>).</item>
/// <item>Values the limiters throw on at the first request: <c>PermitLimit</c> and
/// <c>SegmentsPerWindow</c> must be positive, <c>Window</c> longer than zero.</item>
/// </list>
/// Checks every <see cref="RateLimitPolicyOptions" /> property, so new policies are covered without
/// changes here.
/// </summary>
public class RateLimitingOptionsSetup(IConfiguration config, ILogger<RateLimitingOptionsSetup> logger) : IConfigureOptions<RateLimitingOptions>
{
    public static readonly EventId InvalidConfigEvent = new(0, "RATE_LIMITING_CONFIG_INVALID");

    private static readonly PropertyInfo[] policies = [.. typeof(RateLimitingOptions)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(RateLimitPolicyOptions))];

    private static readonly string[] policyKeys = [.. typeof(RateLimitPolicyOptions)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => p.Name)];

    private readonly IConfiguration config = config;
    private readonly ILogger<RateLimitingOptionsSetup> logger = logger;

    public void Configure(RateLimitingOptions options)
    {
        var section = config.GetSection(RateLimitingOptions.SECTION);

        foreach (var unknown in section.GetChildren().Where(c => !policies.Any(p => KeyEquals(p.Name, c.Key))))
        {
            logger.LogError(InvalidConfigEvent, "{Key} isn't a rate limiting policy. It's ignored.", unknown.Path);
        }

        foreach (var property in policies)
        {
            var policySection = section.GetSection(property.Name);
            if (!policySection.Exists())
            {
                continue;
            }

            foreach (var unknown in policySection.GetChildren().Where(c => !policyKeys.Any(k => KeyEquals(k, c.Key))))
            {
                logger.LogError(InvalidConfigEvent, "{Key} isn't a rate limiting setting. It's ignored.", unknown.Path);
            }

            var defaults = (RateLimitPolicyOptions)property.GetValue(options)!;
            var policy = new RateLimitPolicyOptions(defaults.PermitLimit, defaults.Window, defaults.SegmentsPerWindow);

            try
            {
                policySection.Bind(policy);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(InvalidConfigEvent, "{Key} has a value that can't be read ({Error}). The default is used.", policySection.Path, ex.Message);
                continue;
            }

            var problems = Validate(policy);
            if (problems.Count > 0)
            {
                logger.LogError(InvalidConfigEvent, "{Key} is invalid: {Problems} The default is used.", policySection.Path, string.Join(" ", problems));
                continue;
            }

            property.SetValue(options, policy);
        }
    }

    private static List<string> Validate(RateLimitPolicyOptions policy)
    {
        var problems = new List<string>();

        if (policy.PermitLimit <= 0)
        {
            problems.Add($"PermitLimit must be greater than 0 (was {policy.PermitLimit}).");
        }

        if (policy.Window <= TimeSpan.Zero)
        {
            problems.Add($"Window must be greater than 00:00:00 (was {policy.Window}).");
        }

        if (policy.SegmentsPerWindow is <= 0)
        {
            problems.Add($"SegmentsPerWindow must be greater than 0 or omitted (was {policy.SegmentsPerWindow}).");
        }

        return problems;
    }

    private static bool KeyEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Reads <see cref="RateLimitingOptions" /> at startup, so config problems
/// (<see cref="RateLimitingOptionsSetup" />) are logged then, not at the first rate limited request.
/// </summary>
public class RateLimitingOptionsStartupCheck(IOptions<RateLimitingOptions> options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = options.Value;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
